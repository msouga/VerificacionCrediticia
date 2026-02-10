using System.Text.Json;
using Microsoft.Extensions.Logging;
using VerificacionCrediticia.Core.DTOs;
using VerificacionCrediticia.Core.Entities;
using VerificacionCrediticia.Core.Enums;
using VerificacionCrediticia.Core.Interfaces;

namespace VerificacionCrediticia.Core.Services;

public class ExpedienteService : IExpedienteService
{
    private readonly IExpedienteRepository _expedienteRepo;
    private readonly IDocumentoProcesadoRepository _documentoRepo;
    private readonly ITipoDocumentoRepository _tipoDocumentoRepo;
    private readonly IDocumentIntelligenceService _documentIntelligence;
    private readonly IMotorReglasService _motorReglas;
    private readonly IBlobStorageService _blobStorage;
    private readonly ILogger<ExpedienteService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public ExpedienteService(
        IExpedienteRepository expedienteRepo,
        IDocumentoProcesadoRepository documentoRepo,
        ITipoDocumentoRepository tipoDocumentoRepo,
        IDocumentIntelligenceService documentIntelligence,
        IMotorReglasService motorReglas,
        IBlobStorageService blobStorage,
        ILogger<ExpedienteService> logger)
    {
        _expedienteRepo = expedienteRepo;
        _documentoRepo = documentoRepo;
        _tipoDocumentoRepo = tipoDocumentoRepo;
        _documentIntelligence = documentIntelligence;
        _motorReglas = motorReglas;
        _blobStorage = blobStorage;
        _logger = logger;
    }

    public async Task<ExpedienteDto> CrearExpedienteAsync(CrearExpedienteRequest request)
    {
        var expediente = new Expediente
        {
            Descripcion = request.Descripcion,
            Estado = EstadoExpediente.Iniciado,
            FechaCreacion = DateTime.UtcNow
        };

        var created = await _expedienteRepo.CreateAsync(expediente);
        _logger.LogInformation("Expediente {Id} creado: {Descripcion}", created.Id, created.Descripcion);

        return await BuildExpedienteDtoAsync(created.Id);
    }

    public async Task<ListaExpedientesResponse> ListarExpedientesAsync(int pagina, int tamanoPagina)
    {
        var (items, total) = await _expedienteRepo.GetPaginadoAsync(pagina, tamanoPagina);
        var tiposObligatorios = await _tipoDocumentoRepo.GetObligatoriosAsync();

        var resumen = items.Select(exp =>
        {
            var docsProcesados = exp.Documentos
                .Where(d => d.Estado == EstadoDocumento.Procesado)
                .Select(d => d.TipoDocumentoId)
                .ToHashSet();

            return new ExpedienteResumenDto
            {
                Id = exp.Id,
                Descripcion = exp.Descripcion,
                DniSolicitante = exp.DniSolicitante,
                NombresSolicitante = exp.NombresSolicitante,
                RucEmpresa = exp.RucEmpresa,
                RazonSocialEmpresa = exp.RazonSocialEmpresa,
                Estado = exp.Estado,
                FechaCreacion = exp.FechaCreacion,
                DocumentosObligatoriosCompletos = tiposObligatorios.Count(t => docsProcesados.Contains(t.Id)),
                TotalDocumentosObligatorios = tiposObligatorios.Count
            };
        }).ToList();

        return new ListaExpedientesResponse
        {
            Items = resumen,
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }

    public async Task<ExpedienteDto> ActualizarExpedienteAsync(int id, ActualizarExpedienteRequest request)
    {
        var expediente = await _expedienteRepo.GetByIdTrackingAsync(id)
            ?? throw new KeyNotFoundException($"Expediente {id} no encontrado");

        expediente.Descripcion = request.Descripcion;
        await _expedienteRepo.UpdateAsync(expediente);

        return await BuildExpedienteDtoAsync(id);
    }

    public async Task EliminarExpedientesAsync(List<int> ids)
    {
        await _expedienteRepo.DeleteManyAsync(ids);
        _logger.LogInformation("Expedientes eliminados: {Ids}", string.Join(", ", ids));
    }

    public async Task<ExpedienteDto?> GetExpedienteAsync(int id)
    {
        var expediente = await _expedienteRepo.GetByIdWithDocumentosAsync(id);
        if (expediente == null) return null;

        return await BuildExpedienteDtoAsync(expediente);
    }

    public async Task<DocumentoProcesadoResumenDto> SubirDocumentoAsync(
        int expedienteId, string codigoTipo, Stream stream, string fileName)
    {
        var expediente = await _expedienteRepo.GetByIdWithDocumentosAsync(expedienteId)
            ?? throw new KeyNotFoundException($"Expediente {expedienteId} no encontrado");

        var tipoDocumento = await _tipoDocumentoRepo.GetByCodigoAsync(codigoTipo)
            ?? throw new KeyNotFoundException($"Tipo de documento '{codigoTipo}' no encontrado");

        // Si ya existe un documento de este tipo, eliminar (incluido el blob)
        var existente = expediente.Documentos.FirstOrDefault(d => d.TipoDocumentoId == tipoDocumento.Id);
        if (existente != null)
        {
            if (!string.IsNullOrEmpty(existente.BlobUri))
            {
                await _blobStorage.DeleteAsync(existente.BlobUri);
            }
            await _documentoRepo.DeleteAsync(existente.Id);
            _logger.LogInformation("Documento existente {DocId} reemplazado en expediente {ExpId}",
                existente.Id, expedienteId);
        }

        // Subir a blob storage
        var blobPath = $"documentos/{expedienteId}/{codigoTipo}/{Guid.NewGuid()}_{fileName}";
        var contentType = ObtenerContentType(fileName);
        var blobUri = await _blobStorage.UploadAsync(blobPath, stream, contentType);

        // Crear registro con estado Subido
        var documento = new DocumentoProcesado
        {
            ExpedienteId = expedienteId,
            TipoDocumentoId = tipoDocumento.Id,
            NombreArchivo = fileName,
            Estado = EstadoDocumento.Subido,
            FechaProcesado = DateTime.UtcNow,
            BlobUri = blobUri
        };
        documento = await _documentoRepo.CreateAsync(documento);

        // Actualizar estado del expediente a EnProceso
        if (expediente.Estado == EstadoExpediente.Iniciado)
        {
            expediente.Estado = EstadoExpediente.EnProceso;
            await _expedienteRepo.UpdateAsync(expediente);
        }

        _logger.LogInformation("Documento {CodigoTipo} subido a blob para expediente {ExpId}: {BlobUri}",
            codigoTipo, expedienteId, blobUri);

        return new DocumentoProcesadoResumenDto
        {
            Id = documento.Id,
            TipoDocumentoId = documento.TipoDocumentoId,
            CodigoTipoDocumento = codigoTipo,
            NombreTipoDocumento = tipoDocumento.Nombre,
            NombreArchivo = documento.NombreArchivo,
            FechaProcesado = documento.FechaProcesado,
            Estado = documento.Estado,
            ConfianzaPromedio = null,
            ErrorMensaje = null
        };
    }

    public async Task<DocumentoProcesadoResumenDto> ReemplazarDocumentoSubidoAsync(
        int expedienteId, int documentoId, Stream stream, string fileName)
    {
        var docExistente = await _documentoRepo.GetByIdAsync(documentoId)
            ?? throw new KeyNotFoundException($"Documento {documentoId} no encontrado");

        if (docExistente.ExpedienteId != expedienteId)
            throw new InvalidOperationException("El documento no pertenece al expediente indicado");

        var tipoDocumento = docExistente.TipoDocumento;

        // Eliminar blob anterior
        if (!string.IsNullOrEmpty(docExistente.BlobUri))
        {
            await _blobStorage.DeleteAsync(docExistente.BlobUri);
        }

        // Eliminar registro anterior
        await _documentoRepo.DeleteAsync(documentoId);

        // Subir nuevo documento
        return await SubirDocumentoAsync(expedienteId, tipoDocumento.Codigo, stream, fileName);
    }

    public async Task<ExpedienteDto> EvaluarExpedienteAsync(
        int expedienteId,
        IProgress<ProgresoEvaluacionDto>? progreso = null,
        CancellationToken cancellationToken = default)
    {
        // Usar tracking para poder persistir cambios (documentos + resultado evaluacion)
        var expediente = await _expedienteRepo.GetByIdWithDocumentosTrackingAsync(expedienteId)
            ?? throw new KeyNotFoundException($"Expediente {expedienteId} no encontrado");

        // Obtener documentos pendientes de procesamiento (Subido)
        var documentosSubidos = expediente.Documentos
            .Where(d => d.Estado == EstadoDocumento.Subido)
            .ToList();

        var totalDocumentos = documentosSubidos.Count;
        var errores = new List<string>();

        // Procesar cada documento con Content Understanding
        for (var i = 0; i < documentosSubidos.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var doc = documentosSubidos[i];
            var codigoTipo = doc.TipoDocumento?.Codigo ?? "";
            var docIndex = i + 1;

            progreso?.Report(new ProgresoEvaluacionDto
            {
                Archivo = doc.NombreArchivo,
                Paso = $"Descargando {doc.NombreArchivo}...",
                DocumentoActual = docIndex,
                TotalDocumentos = totalDocumentos
            });

            try
            {
                // Descargar del blob
                using var stream = await _blobStorage.DownloadAsync(doc.BlobUri!);

                progreso?.Report(new ProgresoEvaluacionDto
                {
                    Archivo = doc.NombreArchivo,
                    Paso = $"Procesando con IA: {doc.TipoDocumento?.Nombre ?? codigoTipo}...",
                    DocumentoActual = docIndex,
                    TotalDocumentos = totalDocumentos
                });

                // Actualizar estado a Procesando
                doc.Estado = EstadoDocumento.Procesando;
                await _documentoRepo.UpdateAsync(doc);

                // Crear adaptador de progreso: mensajes de polling de CU -> ProgresoEvaluacionDto
                IProgress<string>? progresoDetalle = progreso != null
                    ? new Progress<string>(mensaje => progreso.Report(new ProgresoEvaluacionDto
                    {
                        Archivo = doc.NombreArchivo,
                        Paso = $"Procesando con IA: {doc.TipoDocumento?.Nombre ?? codigoTipo}...",
                        Detalle = mensaje,
                        DocumentoActual = docIndex,
                        TotalDocumentos = totalDocumentos
                    }))
                    : null;

                // Procesar con Content Understanding
                var resultado = await ProcesarConReintento(codigoTipo, stream, doc.NombreArchivo, docIndex, totalDocumentos, progreso, progresoDetalle, cancellationToken);

                // Guardar resultado
                doc.DatosExtraidosJson = JsonSerializer.Serialize(resultado, JsonOptions);
                doc.Estado = EstadoDocumento.Procesado;
                doc.ConfianzaPromedio = ObtenerConfianzaDeResultado(resultado);
                doc.ErrorMensaje = null;
                await _documentoRepo.UpdateAsync(doc);

                // Actualizar datos del expediente
                await ActualizarDatosExpedienteAsync(expediente, codigoTipo, resultado);

                progreso?.Report(new ProgresoEvaluacionDto
                {
                    Archivo = doc.NombreArchivo,
                    Paso = "Documento procesado correctamente",
                    DocumentoActual = docIndex,
                    TotalDocumentos = totalDocumentos
                });
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando documento {DocId} ({Tipo}) del expediente {ExpId}",
                    doc.Id, codigoTipo, expedienteId);

                doc.Estado = EstadoDocumento.Error;
                doc.ErrorMensaje = ex.Message;
                await _documentoRepo.UpdateAsync(doc);

                errores.Add($"{doc.NombreArchivo}: {ex.Message}");
            }
        }

        // Si hubo errores, lanzar excepcion con resumen
        if (errores.Count > 0)
        {
            throw new InvalidOperationException(
                $"Error procesando {errores.Count} documento(s): {string.Join("; ", errores)}");
        }

        // Verificar que todos los obligatorios esten procesados (ya tenemos el expediente trackeado)
        var tiposObligatorios = await _tipoDocumentoRepo.GetObligatoriosAsync();
        var docsProcesados = expediente.Documentos
            .Where(d => d.Estado == EstadoDocumento.Procesado)
            .Select(d => d.TipoDocumentoId)
            .ToHashSet();

        var faltantes = tiposObligatorios
            .Where(t => !docsProcesados.Contains(t.Id))
            .Select(t => t.Nombre)
            .ToList();

        if (faltantes.Count > 0)
        {
            throw new InvalidOperationException(
                $"Faltan documentos obligatorios: {string.Join(", ", faltantes)}");
        }

        progreso?.Report(new ProgresoEvaluacionDto
        {
            Archivo = "",
            Paso = "Evaluando reglas crediticias...",
            DocumentoActual = totalDocumentos,
            TotalDocumentos = totalDocumentos
        });

        // Extraer datos y ejecutar motor de reglas
        var datosEvaluacion = ExtraerDatosParaEvaluacion(expediente);
        var resultadoReglas = await _motorReglas.EvaluarAsync(datosEvaluacion);

        // Persistir resultado
        var resultadoPersistido = new ResultadoEvaluacionPersistido
        {
            ExpedienteId = expedienteId,
            ScoreFinal = resultadoReglas.PuntajeFinal,
            Recomendacion = resultadoReglas.Recomendacion,
            NivelRiesgo = resultadoReglas.NivelRiesgo,
            ResultadoCompletoJson = JsonSerializer.Serialize(resultadoReglas, JsonOptions),
            FechaEvaluacion = DateTime.UtcNow
        };

        if (expediente.ResultadoEvaluacion != null)
        {
            expediente.ResultadoEvaluacion.ScoreFinal = resultadoPersistido.ScoreFinal;
            expediente.ResultadoEvaluacion.Recomendacion = resultadoPersistido.Recomendacion;
            expediente.ResultadoEvaluacion.NivelRiesgo = resultadoPersistido.NivelRiesgo;
            expediente.ResultadoEvaluacion.ResultadoCompletoJson = resultadoPersistido.ResultadoCompletoJson;
            expediente.ResultadoEvaluacion.FechaEvaluacion = resultadoPersistido.FechaEvaluacion;
        }
        else
        {
            expediente.ResultadoEvaluacion = resultadoPersistido;
        }

        expediente.Estado = EstadoExpediente.Evaluado;
        expediente.FechaEvaluacion = DateTime.UtcNow;
        await _expedienteRepo.UpdateAsync(expediente);

        _logger.LogInformation(
            "Expediente {Id} evaluado: {Recomendacion}, Score: {Score}, Riesgo: {Riesgo}",
            expedienteId, resultadoReglas.Recomendacion, resultadoReglas.PuntajeFinal, resultadoReglas.NivelRiesgo);

        return await BuildExpedienteDtoAsync(expedienteId);
    }

    public async Task<List<TipoDocumentoDto>> GetTiposDocumentoAsync()
    {
        var tipos = await _tipoDocumentoRepo.GetActivosAsync();
        return tipos.Select(t => new TipoDocumentoDto
        {
            Id = t.Id,
            Nombre = t.Nombre,
            Codigo = t.Codigo,
            AnalyzerId = t.AnalyzerId,
            EsObligatorio = t.EsObligatorio,
            Activo = t.Activo,
            Orden = t.Orden,
            Descripcion = t.Descripcion
        }).ToList();
    }

    // Metodos privados

    private async Task<object> ProcesarConReintento(
        string codigoTipo, Stream stream, string fileName,
        int docIndex, int totalDocumentos,
        IProgress<ProgresoEvaluacionDto>? progreso,
        IProgress<string>? progresoDetalle,
        CancellationToken cancellationToken)
    {
        try
        {
            return await ProcesarSegunTipoAsync(codigoTipo, stream, fileName, cancellationToken, progresoDetalle);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Primer intento fallido para {Archivo}, reintentando...", fileName);

            progreso?.Report(new ProgresoEvaluacionDto
            {
                Archivo = fileName,
                Paso = "Reintentando procesamiento...",
                DocumentoActual = docIndex,
                TotalDocumentos = totalDocumentos
            });

            cancellationToken.ThrowIfCancellationRequested();

            // Reiniciar posicion del stream si es posible
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            return await ProcesarSegunTipoAsync(codigoTipo, stream, fileName, cancellationToken, progresoDetalle);
        }
    }

    private async Task<object> ProcesarSegunTipoAsync(
        string codigoTipo, Stream documentStream, string fileName,
        CancellationToken cancellationToken = default,
        IProgress<string>? progreso = null)
    {
        return codigoTipo switch
        {
            "DNI" => await _documentIntelligence.ProcesarDocumentoIdentidadAsync(documentStream, fileName, cancellationToken),
            "VIGENCIA_PODER" => await _documentIntelligence.ProcesarVigenciaPoderAsync(documentStream, fileName, cancellationToken, progreso),
            "BALANCE_GENERAL" => await _documentIntelligence.ProcesarBalanceGeneralAsync(documentStream, fileName, cancellationToken, progreso),
            "ESTADO_RESULTADOS" => await _documentIntelligence.ProcesarEstadoResultadosAsync(documentStream, fileName, cancellationToken, progreso),
            "FICHA_RUC" => await _documentIntelligence.ProcesarFichaRucAsync(documentStream, fileName, cancellationToken, progreso),
            _ => throw new NotSupportedException($"Tipo de documento '{codigoTipo}' no tiene procesamiento implementado")
        };
    }

    private async Task ActualizarDatosExpedienteAsync(Expediente expediente, string codigoTipo, object resultado)
    {
        bool actualizado = false;

        if (codigoTipo == "DNI" && resultado is DocumentoIdentidadDto dni)
        {
            if (string.IsNullOrEmpty(expediente.DniSolicitante) && !string.IsNullOrEmpty(dni.NumeroDocumento))
            {
                expediente.DniSolicitante = dni.NumeroDocumento;
                actualizado = true;
            }
            if (string.IsNullOrEmpty(expediente.NombresSolicitante) && !string.IsNullOrEmpty(dni.Nombres))
            {
                expediente.NombresSolicitante = dni.Nombres;
                actualizado = true;
            }
            if (string.IsNullOrEmpty(expediente.ApellidosSolicitante) && !string.IsNullOrEmpty(dni.Apellidos))
            {
                expediente.ApellidosSolicitante = dni.Apellidos;
                actualizado = true;
            }
        }

        if (codigoTipo == "VIGENCIA_PODER" && resultado is VigenciaPoderDto vigencia)
        {
            if (string.IsNullOrEmpty(expediente.RazonSocialEmpresa) && !string.IsNullOrEmpty(vigencia.RazonSocial))
            {
                expediente.RazonSocialEmpresa = vigencia.RazonSocial;
                actualizado = true;
            }
            if (string.IsNullOrEmpty(expediente.RucEmpresa) && !string.IsNullOrEmpty(vigencia.Ruc))
            {
                expediente.RucEmpresa = vigencia.Ruc;
                actualizado = true;
            }
        }

        if (codigoTipo == "BALANCE_GENERAL" && resultado is BalanceGeneralDto balance)
        {
            if (string.IsNullOrEmpty(expediente.RazonSocialEmpresa) && !string.IsNullOrEmpty(balance.RazonSocial))
            {
                expediente.RazonSocialEmpresa = balance.RazonSocial;
                actualizado = true;
            }
        }

        if (codigoTipo == "ESTADO_RESULTADOS" && resultado is EstadoResultadosDto estadoRes)
        {
            if (string.IsNullOrEmpty(expediente.RazonSocialEmpresa) && !string.IsNullOrEmpty(estadoRes.RazonSocial))
            {
                expediente.RazonSocialEmpresa = estadoRes.RazonSocial;
                actualizado = true;
            }
        }

        if (codigoTipo == "FICHA_RUC" && resultado is FichaRucDto fichaRuc)
        {
            if (string.IsNullOrEmpty(expediente.RucEmpresa) && !string.IsNullOrEmpty(fichaRuc.Ruc))
            {
                expediente.RucEmpresa = fichaRuc.Ruc;
                actualizado = true;
            }
            if (string.IsNullOrEmpty(expediente.RazonSocialEmpresa) && !string.IsNullOrEmpty(fichaRuc.RazonSocial))
            {
                expediente.RazonSocialEmpresa = fichaRuc.RazonSocial;
                actualizado = true;
            }
        }

        if (actualizado)
        {
            await _expedienteRepo.UpdateAsync(expediente);
        }
    }

    private static decimal? ObtenerConfianzaDeResultado(object resultado)
    {
        return resultado switch
        {
            DocumentoIdentidadDto dni => (decimal)dni.ConfianzaPromedio,
            VigenciaPoderDto vp => (decimal)vp.ConfianzaPromedio,
            BalanceGeneralDto bg => (decimal)bg.ConfianzaPromedio,
            EstadoResultadosDto er => er.ConfianzaPromedio,
            FichaRucDto fr => (decimal)fr.ConfianzaPromedio,
            _ => null
        };
    }

    private Dictionary<string, object> ExtraerDatosParaEvaluacion(Expediente expediente)
    {
        var datos = new Dictionary<string, object>();

        foreach (var documento in expediente.Documentos.Where(d => d.Estado == EstadoDocumento.Procesado))
        {
            if (string.IsNullOrEmpty(documento.DatosExtraidosJson)) continue;

            var codigoTipo = documento.TipoDocumento?.Codigo ?? "";

            try
            {
                switch (codigoTipo)
                {
                    case "BALANCE_GENERAL":
                        var balance = JsonSerializer.Deserialize<BalanceGeneralDto>(
                            documento.DatosExtraidosJson, JsonOptions);
                        if (balance != null)
                        {
                            if (balance.RatioLiquidez.HasValue)
                                datos["Liquidez"] = balance.RatioLiquidez.Value;
                            if (balance.RatioEndeudamiento.HasValue)
                                datos["Endeudamiento"] = balance.RatioEndeudamiento.Value;
                            if (balance.RatioSolvencia.HasValue)
                                datos["Solvencia"] = balance.RatioSolvencia.Value;
                            if (balance.CapitalTrabajo.HasValue)
                                datos["CapitalTrabajo"] = balance.CapitalTrabajo.Value;
                        }
                        break;

                    case "ESTADO_RESULTADOS":
                        var estado = JsonSerializer.Deserialize<EstadoResultadosDto>(
                            documento.DatosExtraidosJson, JsonOptions);
                        if (estado != null)
                        {
                            if (estado.MargenBruto.HasValue)
                                datos["MargenBruto"] = estado.MargenBruto.Value / 100;
                            if (estado.MargenOperativo.HasValue)
                                datos["MargenOperativo"] = estado.MargenOperativo.Value / 100;
                            if (estado.MargenNeto.HasValue)
                                datos["MargenNeto"] = estado.MargenNeto.Value / 100;
                            if (estado.VentasNetas.HasValue)
                                datos["VentasNetas"] = estado.VentasNetas.Value;
                            if (estado.UtilidadNeta.HasValue)
                                datos["UtilidadNeta"] = estado.UtilidadNeta.Value;
                        }
                        break;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "No se pudo deserializar datos del documento {DocId} ({Tipo})",
                    documento.Id, codigoTipo);
            }
        }

        datos.TryAdd("ScoreCrediticio", 500m);
        datos.TryAdd("DeudaVencida", 0m);

        return datos;
    }

    private async Task<ExpedienteDto> BuildExpedienteDtoAsync(int expedienteId)
    {
        var expediente = await _expedienteRepo.GetByIdWithDocumentosAsync(expedienteId);
        if (expediente == null) throw new KeyNotFoundException($"Expediente {expedienteId} no encontrado");

        return await BuildExpedienteDtoAsync(expediente);
    }

    private async Task<ExpedienteDto> BuildExpedienteDtoAsync(Expediente expediente)
    {
        var tiposRequeridos = await _tipoDocumentoRepo.GetActivosAsync();
        var tiposObligatorios = tiposRequeridos.Where(t => t.EsObligatorio).ToList();

        // Considerar tanto Procesado como Subido para el conteo de documentos obligatorios
        var docsListos = expediente.Documentos
            .Where(d => d.Estado == EstadoDocumento.Procesado || d.Estado == EstadoDocumento.Subido)
            .Select(d => d.TipoDocumentoId)
            .ToHashSet();

        var obligatoriosCompletos = tiposObligatorios.Count(t => docsListos.Contains(t.Id));

        var dto = new ExpedienteDto
        {
            Id = expediente.Id,
            Descripcion = expediente.Descripcion,
            DniSolicitante = expediente.DniSolicitante,
            NombresSolicitante = expediente.NombresSolicitante,
            ApellidosSolicitante = expediente.ApellidosSolicitante,
            RucEmpresa = expediente.RucEmpresa,
            RazonSocialEmpresa = expediente.RazonSocialEmpresa,
            Estado = expediente.Estado,
            FechaCreacion = expediente.FechaCreacion,
            FechaEvaluacion = expediente.FechaEvaluacion,
            DocumentosObligatoriosCompletos = obligatoriosCompletos,
            TotalDocumentosObligatorios = tiposObligatorios.Count,
            PuedeEvaluar = obligatoriosCompletos == tiposObligatorios.Count
        };

        dto.Documentos = expediente.Documentos
            .OrderBy(d => d.TipoDocumento?.Orden ?? 999)
            .Select(d => new DocumentoProcesadoResumenDto
            {
                Id = d.Id,
                TipoDocumentoId = d.TipoDocumentoId,
                CodigoTipoDocumento = d.TipoDocumento?.Codigo ?? "",
                NombreTipoDocumento = d.TipoDocumento?.Nombre ?? "",
                NombreArchivo = d.NombreArchivo,
                FechaProcesado = d.FechaProcesado,
                Estado = d.Estado,
                ConfianzaPromedio = d.ConfianzaPromedio,
                ErrorMensaje = d.ErrorMensaje
            }).ToList();

        dto.TiposDocumentoRequeridos = tiposRequeridos
            .Select(t => new TipoDocumentoDto
            {
                Id = t.Id,
                Nombre = t.Nombre,
                Codigo = t.Codigo,
                AnalyzerId = t.AnalyzerId,
                EsObligatorio = t.EsObligatorio,
                Activo = t.Activo,
                Orden = t.Orden,
                Descripcion = t.Descripcion
            }).ToList();

        if (expediente.ResultadoEvaluacion != null)
        {
            var resultado = expediente.ResultadoEvaluacion;

            ResultadoMotorReglas? resultadoCompleto = null;
            try
            {
                resultadoCompleto = JsonSerializer.Deserialize<ResultadoMotorReglas>(
                    resultado.ResultadoCompletoJson, JsonOptions);
            }
            catch (JsonException)
            {
            }

            dto.ResultadoEvaluacion = new ResultadoEvaluacionExpedienteDto
            {
                ScoreFinal = resultado.ScoreFinal,
                Recomendacion = resultado.Recomendacion,
                NivelRiesgo = resultado.NivelRiesgo,
                Resumen = resultadoCompleto?.Resumen ?? "",
                FechaEvaluacion = resultado.FechaEvaluacion,
                ReglasAplicadas = resultadoCompleto?.ReglasAplicadas
                    .Select(r => new ReglaAplicadaExpedienteDto
                    {
                        NombreRegla = r.NombreRegla,
                        CampoEvaluado = r.CampoEvaluado,
                        Operador = r.OperadorUtilizado,
                        ValorEsperado = r.ValorEsperado,
                        ValorReal = r.ValorReal,
                        Cumplida = r.Cumplida,
                        Mensaje = r.Mensaje,
                        ResultadoRegla = r.ResultadoRegla
                    }).ToList() ?? new()
            };
        }

        return dto;
    }

    private static string ObtenerContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".bmp" => "image/bmp",
            ".tiff" => "image/tiff",
            _ => "application/octet-stream"
        };
    }
}
