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
    private readonly IDocumentProcessingService _processingService;
    private readonly IMotorReglasService _motorReglas;
    private readonly IValidacionCruzadaService _validacionCruzada;
    private readonly INarrativaEvaluacionService _narrativaService;
    private readonly IExploradorRedService _exploradorRed;
    private readonly IBlobStorageService _blobStorage;
    private readonly IDocumentProcessingQueue _processingQueue;
    private readonly IParametroLineaCreditoRepository _parametroLineaCreditoRepo;
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
        IDocumentProcessingService processingService,
        IMotorReglasService motorReglas,
        IValidacionCruzadaService validacionCruzada,
        INarrativaEvaluacionService narrativaService,
        IExploradorRedService exploradorRed,
        IBlobStorageService blobStorage,
        IDocumentProcessingQueue processingQueue,
        IParametroLineaCreditoRepository parametroLineaCreditoRepo,
        ILogger<ExpedienteService> logger)
    {
        _expedienteRepo = expedienteRepo;
        _documentoRepo = documentoRepo;
        _tipoDocumentoRepo = tipoDocumentoRepo;
        _processingService = processingService;
        _motorReglas = motorReglas;
        _validacionCruzada = validacionCruzada;
        _narrativaService = narrativaService;
        _exploradorRed = exploradorRed;
        _blobStorage = blobStorage;
        _processingQueue = processingQueue;
        _parametroLineaCreditoRepo = parametroLineaCreditoRepo;
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
        var tiposObligatorios = await _tipoDocumentoRepo.GetObligatoriosAsync();
        var tipoObligatorioIds = tiposObligatorios.Select(t => t.Id).ToList();

        var (items, total) = await _expedienteRepo.GetPaginadoResumenAsync(pagina, tamanoPagina, tipoObligatorioIds);

        return new ListaExpedientesResponse
        {
            Items = items,
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

        // Encolar para procesamiento en background
        await _processingQueue.EnqueueAsync(new DocumentoProcesarMessage(
            expedienteId, documento.Id, codigoTipo, blobUri, fileName));

        _logger.LogInformation("Documento {CodigoTipo} subido a blob y encolado para expediente {ExpId}: {BlobUri}",
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
        // Lectura sin tracking para verificar estado inicial
        var expedienteSnapshot = await _expedienteRepo.GetByIdWithDocumentosAsync(expedienteId)
            ?? throw new KeyNotFoundException($"Expediente {expedienteId} no encontrado");

        // Documentos que aun no estan procesados
        var totalPendientes = expedienteSnapshot.Documentos
            .Count(d => d.Estado == EstadoDocumento.Subido || d.Estado == EstadoDocumento.Procesando);

        var errores = new List<string>();

        if (totalPendientes > 0)
        {
            progreso?.Report(new ProgresoEvaluacionDto
            {
                Archivo = "",
                Paso = $"Esperando procesamiento de {totalPendientes} documento(s)...",
                DocumentoActual = 0,
                TotalDocumentos = totalPendientes
            });

            // Esperar a que el background processor termine (polling con AsNoTracking)
            var maxEspera = TimeSpan.FromMinutes(5);
            var inicio = DateTime.UtcNow;
            var docIndex = 0;

            while (DateTime.UtcNow - inicio < maxEspera)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(2000, cancellationToken);

                // AsNoTracking: ve los cambios reales de la BD hechos por el background processor
                var snapshot = await _expedienteRepo.GetByIdWithDocumentosAsync(expedienteId);
                if (snapshot == null) break;

                var pendientes = snapshot.Documentos
                    .Count(d => d.Estado == EstadoDocumento.Subido || d.Estado == EstadoDocumento.Procesando);

                var procesados = totalPendientes - pendientes;
                if (procesados > docIndex)
                {
                    docIndex = procesados;
                    progreso?.Report(new ProgresoEvaluacionDto
                    {
                        Archivo = "",
                        Paso = $"Procesados {procesados} de {totalPendientes} documento(s)...",
                        DocumentoActual = procesados,
                        TotalDocumentos = totalPendientes
                    });
                }

                if (pendientes == 0) break;
            }
        }

        // Re-leer con tracking para persistir cambios (fallback + reglas)
        var expediente = await _expedienteRepo.GetByIdWithDocumentosTrackingAsync(expedienteId)
            ?? throw new KeyNotFoundException($"Expediente {expedienteId} no encontrado");

        // Fallback sincrono: si aun quedan docs en Subido, procesarlos aqui
        var docsSubidos = expediente.Documentos
            .Where(d => d.Estado == EstadoDocumento.Subido)
            .ToList();

        for (var i = 0; i < docsSubidos.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var doc = docsSubidos[i];
            var codigoTipo = doc.TipoDocumento?.Codigo ?? "";
            var idx = i + 1;

            progreso?.Report(new ProgresoEvaluacionDto
            {
                Archivo = doc.NombreArchivo,
                Paso = $"Procesando (fallback): {doc.TipoDocumento?.Nombre ?? codigoTipo}...",
                DocumentoActual = idx,
                TotalDocumentos = docsSubidos.Count
            });

            try
            {
                await ProcesarDocumentoSincrono(doc, codigoTipo, expediente, idx, docsSubidos.Count, progreso, cancellationToken);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando (fallback) documento {DocId} ({Tipo}) del expediente {ExpId}",
                    doc.Id, codigoTipo, expedienteId);
                doc.Estado = EstadoDocumento.Error;
                doc.ErrorMensaje = ex.Message;
                await _documentoRepo.UpdateAsync(doc);
                errores.Add($"{doc.NombreArchivo}: {ex.Message}");
            }
        }

        // Verificar docs en Procesando (background no termino)
        var docsEnProceso = expediente.Documentos
            .Where(d => d.Estado == EstadoDocumento.Procesando)
            .ToList();

        if (docsEnProceso.Count > 0)
        {
            errores.Add($"{docsEnProceso.Count} documento(s) aun en procesamiento");
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
            .Where(d => d.Estado == EstadoDocumento.Procesado && d.TipoDocumentoId.HasValue)
            .Select(d => d.TipoDocumentoId!.Value)
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

        var totalDocs = expediente.Documentos.Count;

        // Validacion cruzada de documentos
        progreso?.Report(new ProgresoEvaluacionDto
        {
            Archivo = "",
            Paso = "Validando consistencia entre documentos...",
            DocumentoActual = totalDocs,
            TotalDocumentos = totalDocs
        });

        var documentosProcesados = expediente.Documentos
            .Where(d => d.Estado == EstadoDocumento.Procesado)
            .ToList();
        var validacionesCruzadas = _validacionCruzada.ValidarDocumentos(expediente, documentosProcesados);

        // Si hay validaciones criticas fallidas (Rechazar), no evaluar reglas financieras
        var rechazosValidacion = validacionesCruzadas
            .Where(v => !v.Aprobada && v.Severidad == ResultadoRegla.Rechazar)
            .ToList();

        if (rechazosValidacion.Count > 0)
        {
            _logger.LogWarning("Expediente {Id}: validacion cruzada rechaza. {Rechazos}",
                expedienteId, string.Join("; ", rechazosValidacion.Select(r => r.Mensaje)));

            var resultadoRechazado = new ResultadoMotorReglas
            {
                Recomendacion = Recomendacion.Rechazar,
                PuntajeFinal = 0,
                NivelRiesgo = NivelRiesgo.MuyAlto,
                ValidacionesCruzadas = validacionesCruzadas,
                Resumen = $"Rechazado por validacion cruzada: {string.Join("; ", rechazosValidacion.Select(r => r.Nombre))}",
                FechaEvaluacion = DateTime.UtcNow
            };

            var persistidoRechazado = new ResultadoEvaluacionPersistido
            {
                ExpedienteId = expedienteId,
                ScoreFinal = 0,
                Recomendacion = Recomendacion.Rechazar,
                NivelRiesgo = NivelRiesgo.MuyAlto,
                ResultadoCompletoJson = JsonSerializer.Serialize(resultadoRechazado, JsonOptions),
                FechaEvaluacion = DateTime.UtcNow
            };

            if (expediente.ResultadoEvaluacion != null)
            {
                expediente.ResultadoEvaluacion.ScoreFinal = persistidoRechazado.ScoreFinal;
                expediente.ResultadoEvaluacion.Recomendacion = persistidoRechazado.Recomendacion;
                expediente.ResultadoEvaluacion.NivelRiesgo = persistidoRechazado.NivelRiesgo;
                expediente.ResultadoEvaluacion.ResultadoCompletoJson = persistidoRechazado.ResultadoCompletoJson;
                expediente.ResultadoEvaluacion.FechaEvaluacion = persistidoRechazado.FechaEvaluacion;
            }
            else
            {
                expediente.ResultadoEvaluacion = persistidoRechazado;
            }

            expediente.Estado = EstadoExpediente.Evaluado;
            expediente.FechaEvaluacion = DateTime.UtcNow;
            await _expedienteRepo.UpdateAsync(expediente);

            return await BuildExpedienteDtoAsync(expedienteId);
        }

        // Consultar red de relaciones (Equifax)
        ResultadoExploracionDto? exploracionRed = null;
        if (!string.IsNullOrEmpty(expediente.DniSolicitante) && !string.IsNullOrEmpty(expediente.RucEmpresa))
        {
            progreso?.Report(new ProgresoEvaluacionDto
            {
                Archivo = "",
                Paso = "Consultando red de relaciones crediticias...",
                DocumentoActual = totalDocs,
                TotalDocumentos = totalDocs
            });

            try
            {
                exploracionRed = await _exploradorRed.ExplorarRedAsync(
                    expediente.DniSolicitante, expediente.RucEmpresa, 2, cancellationToken);

                _logger.LogInformation(
                    "Red explorada para expediente {Id}: {Nodos} nodos ({Personas} personas, {Empresas} empresas)",
                    expedienteId, exploracionRed.TotalNodos, exploracionRed.TotalPersonas, exploracionRed.TotalEmpresas);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error consultando red de relaciones para expediente {Id}", expedienteId);
            }
        }

        progreso?.Report(new ProgresoEvaluacionDto
        {
            Archivo = "",
            Paso = "Evaluando reglas crediticias...",
            DocumentoActual = totalDocs,
            TotalDocumentos = totalDocs
        });

        // Extraer datos y ejecutar motor de reglas
        var datosEvaluacion = ExtraerDatosParaEvaluacion(expediente);
        var resultadoReglas = await _motorReglas.EvaluarAsync(datosEvaluacion);

        // Agregar validaciones cruzadas y exploracion de red al resultado
        resultadoReglas.ValidacionesCruzadas = validacionesCruzadas;
        resultadoReglas.ExploracionRed = exploracionRed;

        // Cargar parametros (se usan para linea de credito y penalidad red)
        var parametrosLC = await _parametroLineaCreditoRepo.GetAsync(cancellationToken);

        // Aplicar penalidad por red de relaciones
        if (exploracionRed != null)
        {
            var penalidadRed = CalcularPenalidadRed(exploracionRed, parametrosLC);
            if (penalidadRed > 0)
            {
                resultadoReglas.PenalidadRed = penalidadRed;
                resultadoReglas.PuntajeFinal = Math.Max(0, resultadoReglas.PuntajeFinal - penalidadRed);
                RecalcularNivelYRecomendacion(resultadoReglas);

                _logger.LogInformation(
                    "Expediente {Id}: penalidad red = {Penalidad}, score ajustado = {Score}",
                    expedienteId, penalidadRed, resultadoReglas.PuntajeFinal);
            }
        }

        // Calcular recomendacion de linea de credito
        if (resultadoReglas.Recomendacion != Recomendacion.Rechazar)
        {
            resultadoReglas.LineaCredito = CalcularLineaCredito(expediente, parametrosLC);
        }

        // Generar narrativa con IA
        progreso?.Report(new ProgresoEvaluacionDto
        {
            Archivo = "",
            Paso = "Generando informe narrativo...",
            DocumentoActual = totalDocs,
            TotalDocumentos = totalDocs
        });

        try
        {
            resultadoReglas.Resumen = await _narrativaService.GenerarNarrativaAsync(
                expediente, resultadoReglas, datosEvaluacion, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error generando narrativa, usando resumen basico");
            resultadoReglas.Resumen = $"Score: {resultadoReglas.PuntajeFinal:F1}. Recomendacion: {resultadoReglas.Recomendacion}.";
        }

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

    public async Task<List<DocumentoProcesadoResumenDto>> SubirDocumentosBulkAsync(
        int expedienteId, List<(Stream Stream, string FileName)> archivos)
    {
        var expediente = await _expedienteRepo.GetByIdWithDocumentosAsync(expedienteId)
            ?? throw new KeyNotFoundException($"Expediente {expedienteId} no encontrado");

        var resultados = new List<DocumentoProcesadoResumenDto>();

        foreach (var (stream, fileName) in archivos)
        {
            // Subir a blob storage con path AUTO
            var blobPath = $"documentos/{expedienteId}/AUTO/{Guid.NewGuid()}_{fileName}";
            var contentType = ObtenerContentType(fileName);
            var blobUri = await _blobStorage.UploadAsync(blobPath, stream, contentType);

            // Crear registro con TipoDocumentoId = null, estado Subido
            var documento = new DocumentoProcesado
            {
                ExpedienteId = expedienteId,
                TipoDocumentoId = null,
                NombreArchivo = fileName,
                Estado = EstadoDocumento.Subido,
                FechaProcesado = DateTime.UtcNow,
                BlobUri = blobUri
            };
            documento = await _documentoRepo.CreateAsync(documento);

            // Encolar para procesamiento en background con CodigoTipo = "AUTO"
            await _processingQueue.EnqueueAsync(new DocumentoProcesarMessage(
                expedienteId, documento.Id, "AUTO", blobUri, fileName));

            _logger.LogInformation(
                "Documento bulk subido y encolado para expediente {ExpId}: {FileName} -> {BlobUri}",
                expedienteId, fileName, blobUri);

            resultados.Add(new DocumentoProcesadoResumenDto
            {
                Id = documento.Id,
                TipoDocumentoId = null,
                CodigoTipoDocumento = "",
                NombreTipoDocumento = "Clasificando...",
                NombreArchivo = documento.NombreArchivo,
                FechaProcesado = documento.FechaProcesado,
                Estado = documento.Estado,
                ConfianzaPromedio = null,
                ErrorMensaje = null
            });
        }

        // Actualizar estado del expediente a EnProceso
        if (expediente.Estado == EstadoExpediente.Iniciado)
        {
            var expTracking = await _expedienteRepo.GetByIdTrackingAsync(expedienteId);
            if (expTracking != null)
            {
                expTracking.Estado = EstadoExpediente.EnProceso;
                await _expedienteRepo.UpdateAsync(expTracking);
            }
        }

        return resultados;
    }

    public async Task DescartarDocumentoAsync(int expedienteId, int documentoId)
    {
        var doc = await _documentoRepo.GetByIdAsync(documentoId)
            ?? throw new KeyNotFoundException($"Documento {documentoId} no encontrado");

        if (doc.ExpedienteId != expedienteId)
            throw new InvalidOperationException("El documento no pertenece al expediente indicado");

        // Solo permitir descartar documentos en Error o sin tipo asignado
        if (doc.Estado != EstadoDocumento.Error && doc.TipoDocumentoId != null)
            throw new InvalidOperationException("Solo se pueden descartar documentos en error o sin tipo asignado");

        // Eliminar blob
        if (!string.IsNullOrEmpty(doc.BlobUri))
        {
            try { await _blobStorage.DeleteAsync(doc.BlobUri); }
            catch (Exception ex) { _logger.LogWarning(ex, "No se pudo eliminar blob {Uri}", doc.BlobUri); }
        }

        await _documentoRepo.DeleteAsync(documentoId);
        _logger.LogInformation("Documento {DocId} descartado del expediente {ExpId}", documentoId, expedienteId);
    }

    public async Task<DocumentoProcesadoResumenDto> AceptarDocumentoAsync(int expedienteId, int documentoId)
    {
        var doc = await _documentoRepo.GetByIdAsync(documentoId)
            ?? throw new KeyNotFoundException($"Documento {documentoId} no encontrado");

        if (doc.ExpedienteId != expedienteId)
            throw new InvalidOperationException("El documento no pertenece al expediente indicado");

        if (doc.Estado != EstadoDocumento.Error)
            throw new InvalidOperationException("Solo se pueden aceptar documentos en estado Error");

        if (string.IsNullOrEmpty(doc.BlobUri))
            throw new InvalidOperationException("El documento no tiene archivo asociado en blob storage");

        // Re-poner estado a Subido y limpiar error
        doc.Estado = EstadoDocumento.Subido;
        doc.ErrorMensaje = null;
        doc.TipoDocumentoId = null;
        doc.DatosExtraidosJson = null;
        await _documentoRepo.UpdateAsync(doc);

        // Re-encolar para procesamiento
        await _processingQueue.EnqueueAsync(new DocumentoProcesarMessage(
            expedienteId, doc.Id, "AUTO", doc.BlobUri, doc.NombreArchivo));

        _logger.LogInformation("Documento {DocId} aceptado y re-encolado para expediente {ExpId}", documentoId, expedienteId);

        return new DocumentoProcesadoResumenDto
        {
            Id = doc.Id,
            TipoDocumentoId = null,
            CodigoTipoDocumento = "",
            NombreTipoDocumento = "Clasificando...",
            NombreArchivo = doc.NombreArchivo,
            FechaProcesado = doc.FechaProcesado,
            Estado = doc.Estado,
            ConfianzaPromedio = null,
            ErrorMensaje = null
        };
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
            return await _processingService.ProcesarSegunTipoAsync(codigoTipo, stream, fileName, cancellationToken, progresoDetalle);
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

            return await _processingService.ProcesarSegunTipoAsync(codigoTipo, stream, fileName, cancellationToken, progresoDetalle);
        }
    }

    private async Task ProcesarDocumentoSincrono(
        DocumentoProcesado doc, string codigoTipo, Expediente expediente,
        int docIndex, int totalDocumentos,
        IProgress<ProgresoEvaluacionDto>? progreso,
        CancellationToken cancellationToken)
    {
        using var stream = await _blobStorage.DownloadAsync(doc.BlobUri!);

        doc.Estado = EstadoDocumento.Procesando;
        await _documentoRepo.UpdateAsync(doc);

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

        var resultado = await ProcesarConReintento(codigoTipo, stream, doc.NombreArchivo, docIndex, totalDocumentos, progreso, progresoDetalle, cancellationToken);

        doc.DatosExtraidosJson = JsonSerializer.Serialize(resultado, JsonOptions);
        doc.Estado = EstadoDocumento.Procesado;
        doc.ConfianzaPromedio = _processingService.ObtenerConfianzaDeResultado(resultado);
        doc.ErrorMensaje = null;
        await _documentoRepo.UpdateAsync(doc);

        await _processingService.ActualizarDatosExpedienteAsync(expediente, codigoTipo, resultado);
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

    private RecomendacionLineaCredito? CalcularLineaCredito(Expediente expediente, ParametroLineaCredito parametros)
    {
        BalanceGeneralDto? balance = null;
        EstadoResultadosDto? estadoResultados = null;

        foreach (var doc in expediente.Documentos.Where(d => d.Estado == EstadoDocumento.Procesado))
        {
            if (string.IsNullOrEmpty(doc.DatosExtraidosJson)) continue;
            var codigo = doc.TipoDocumento?.Codigo ?? "";
            try
            {
                if (codigo == "BALANCE_GENERAL")
                    balance = JsonSerializer.Deserialize<BalanceGeneralDto>(doc.DatosExtraidosJson, JsonOptions);
                else if (codigo == "ESTADO_RESULTADOS")
                    estadoResultados = JsonSerializer.Deserialize<EstadoResultadosDto>(doc.DatosExtraidosJson, JsonOptions);
            }
            catch (JsonException) { }
        }

        if (balance == null) return null;

        var detalles = new List<DetalleCalculoLinea>();
        decimal? montoCapitalTrabajo = null;
        decimal? montoPatrimonio = null;
        decimal? montoUtilidadAnual = null;

        // 1. % del Capital de Trabajo
        if (balance.CapitalTrabajo.HasValue && balance.CapitalTrabajo.Value > 0 && parametros.PorcentajeCapitalTrabajo > 0)
        {
            montoCapitalTrabajo = Math.Round(balance.CapitalTrabajo.Value * (parametros.PorcentajeCapitalTrabajo / 100m), 2);
            detalles.Add(new DetalleCalculoLinea
            {
                Concepto = "Capital de Trabajo",
                ValorBase = balance.CapitalTrabajo.Value,
                Porcentaje = parametros.PorcentajeCapitalTrabajo,
                MontoCalculado = montoCapitalTrabajo
            });
        }

        // 2. % del Patrimonio Neto
        if (balance.TotalPatrimonio.HasValue && balance.TotalPatrimonio.Value > 0 && parametros.PorcentajePatrimonio > 0)
        {
            montoPatrimonio = Math.Round(balance.TotalPatrimonio.Value * (parametros.PorcentajePatrimonio / 100m), 2);
            detalles.Add(new DetalleCalculoLinea
            {
                Concepto = "Patrimonio Neto",
                ValorBase = balance.TotalPatrimonio.Value,
                Porcentaje = parametros.PorcentajePatrimonio,
                MontoCalculado = montoPatrimonio
            });
        }

        // 3. % de Utilidad Neta Anual (capacidad de pago)
        if (estadoResultados?.UtilidadNeta != null && estadoResultados.UtilidadNeta.Value > 0 && parametros.PorcentajeUtilidadNeta > 0)
        {
            montoUtilidadAnual = Math.Round(estadoResultados.UtilidadNeta.Value * (parametros.PorcentajeUtilidadNeta / 100m), 2);
            detalles.Add(new DetalleCalculoLinea
            {
                Concepto = "Utilidad Neta Anual (capacidad de pago)",
                ValorBase = estadoResultados.UtilidadNeta.Value,
                Porcentaje = parametros.PorcentajeUtilidadNeta,
                MontoCalculado = montoUtilidadAnual
            });
        }

        if (detalles.Count == 0) return null;

        // Tomar el minimo de los montos disponibles (criterio conservador)
        var montos = new[] { montoCapitalTrabajo, montoPatrimonio, montoUtilidadAnual }
            .Where(m => m.HasValue)
            .Select(m => m!.Value)
            .ToList();

        var montoMaximo = montos.Min();

        // Determinar moneda del balance
        var moneda = balance.Moneda?.Trim().ToUpper() switch
        {
            "DOLARES" or "USD" or "US$" or "DÓLARES" => "USD",
            _ => "PEN"
        };

        // Justificacion
        var conceptoLimitante = detalles.First(d => d.MontoCalculado == montoMaximo).Concepto;
        var justificacion = $"Linea maxima sugerida basada en el menor de: " +
            string.Join(", ", detalles.Select(d => $"{d.Porcentaje}% de {d.Concepto}")) +
            $". El factor limitante es {conceptoLimitante}.";

        return new RecomendacionLineaCredito
        {
            MontoMaximoSugerido = montoMaximo,
            Moneda = moneda,
            Justificacion = justificacion,
            Detalles = detalles
        };
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
            .Where(d => (d.Estado == EstadoDocumento.Procesado || d.Estado == EstadoDocumento.Subido) && d.TipoDocumentoId.HasValue)
            .Select(d => d.TipoDocumentoId!.Value)
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
                NombreTipoDocumento = d.TipoDocumento?.Nombre ?? (d.TipoDocumentoId == null ? "Clasificando..." : ""),
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
                    }).ToList() ?? new(),
                PenalidadRed = resultadoCompleto?.PenalidadRed ?? 0,
                ValidacionesCruzadas = resultadoCompleto?.ValidacionesCruzadas ?? new(),
                LineaCredito = resultadoCompleto?.LineaCredito,
                ExploracionRed = resultadoCompleto?.ExploracionRed
            };
        }

        return dto;
    }

    private static decimal CalcularPenalidadRed(ResultadoExploracionDto red, ParametroLineaCredito parametros)
    {
        var penalidad = 0m;
        foreach (var (id, nodo) in red.Grafo)
        {
            // Excluir nodos raiz (solicitante y empresa)
            if (id == red.DniSolicitante || id == red.RucEmpresa)
                continue;

            var penalidadBase = nodo.NivelRiesgoTexto?.ToUpper() switch
            {
                var t when t?.Contains("MUY ALTO") == true => 25m,
                var t when t?.Contains("ALTO") == true => 15m,
                var t when t?.Contains("MODERADO") == true => 5m,
                _ => 0m
            };

            if (penalidadBase == 0) continue;

            var pesoNivel = nodo.NivelProfundidad switch
            {
                0 => parametros.PesoRedNivel0,
                1 => parametros.PesoRedNivel1,
                _ => parametros.PesoRedNivel2
            };

            penalidad += penalidadBase * (pesoNivel / 100m);
        }

        return Math.Round(penalidad, 2);
    }

    private static void RecalcularNivelYRecomendacion(ResultadoMotorReglas resultado)
    {
        resultado.NivelRiesgo = resultado.PuntajeFinal switch
        {
            >= 80m => NivelRiesgo.Bajo,
            >= 60m => NivelRiesgo.Moderado,
            >= 40m => NivelRiesgo.Alto,
            _ => NivelRiesgo.MuyAlto
        };

        if (resultado.PuntajeFinal < 40m)
            resultado.Recomendacion = Recomendacion.Rechazar;
        else if (resultado.PuntajeFinal < 80m && resultado.Recomendacion == Recomendacion.Aprobar)
            resultado.Recomendacion = Recomendacion.RevisarManualmente;
    }

    public async Task<EstadisticasExpedientesDto> GetEstadisticasAsync()
    {
        return await _expedienteRepo.GetEstadisticasAsync();
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
