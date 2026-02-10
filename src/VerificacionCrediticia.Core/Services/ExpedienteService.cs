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
        ILogger<ExpedienteService> logger)
    {
        _expedienteRepo = expedienteRepo;
        _documentoRepo = documentoRepo;
        _tipoDocumentoRepo = tipoDocumentoRepo;
        _documentIntelligence = documentIntelligence;
        _motorReglas = motorReglas;
        _logger = logger;
    }

    public async Task<ExpedienteDto> CrearExpedienteAsync(CrearExpedienteRequest request)
    {
        var expediente = new Expediente
        {
            DniSolicitante = request.DniSolicitante,
            RucEmpresa = request.RucEmpresa,
            Estado = EstadoExpediente.Iniciado,
            FechaCreacion = DateTime.UtcNow
        };

        var created = await _expedienteRepo.CreateAsync(expediente);
        _logger.LogInformation("Expediente {Id} creado para DNI {Dni}", created.Id, created.DniSolicitante);

        return await BuildExpedienteDtoAsync(created.Id);
    }

    public async Task<ExpedienteDto?> GetExpedienteAsync(int id)
    {
        var expediente = await _expedienteRepo.GetByIdWithDocumentosAsync(id);
        if (expediente == null) return null;

        return await BuildExpedienteDtoAsync(expediente);
    }

    public async Task<object> ProcesarDocumentoAsync(
        int expedienteId, string codigoTipo,
        Stream documentStream, string fileName,
        IProgress<string>? progreso = null)
    {
        var expediente = await _expedienteRepo.GetByIdWithDocumentosAsync(expedienteId)
            ?? throw new KeyNotFoundException($"Expediente {expedienteId} no encontrado");

        var tipoDocumento = await _tipoDocumentoRepo.GetByCodigoAsync(codigoTipo)
            ?? throw new KeyNotFoundException($"Tipo de documento '{codigoTipo}' no encontrado");

        // Verificar si ya existe un documento de este tipo; si sí, eliminarlo
        var existente = expediente.Documentos.FirstOrDefault(d => d.TipoDocumentoId == tipoDocumento.Id);
        if (existente != null)
        {
            await _documentoRepo.DeleteAsync(existente.Id);
            _logger.LogInformation("Documento existente {DocId} reemplazado en expediente {ExpId}",
                existente.Id, expedienteId);
        }

        // Crear registro del documento
        var documento = new DocumentoProcesado
        {
            ExpedienteId = expedienteId,
            TipoDocumentoId = tipoDocumento.Id,
            NombreArchivo = fileName,
            Estado = EstadoDocumento.Procesando,
            FechaProcesado = DateTime.UtcNow
        };
        documento = await _documentoRepo.CreateAsync(documento);

        try
        {
            // Procesar según el tipo de documento
            var resultado = await ProcesarSegunTipoAsync(codigoTipo, documentStream, fileName, progreso);

            // Guardar resultado
            documento.DatosExtraidosJson = JsonSerializer.Serialize(resultado, JsonOptions);
            documento.Estado = EstadoDocumento.Procesado;
            documento.ConfianzaPromedio = ObtenerConfianzaDeResultado(resultado);

            await _documentoRepo.UpdateAsync(documento);

            // Actualizar datos del expediente si corresponde
            await ActualizarDatosExpedienteAsync(expediente, codigoTipo, resultado);

            // Actualizar estado del expediente
            await ActualizarEstadoExpedienteAsync(expedienteId);

            return resultado;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando documento {CodigoTipo} para expediente {ExpedienteId}",
                codigoTipo, expedienteId);

            documento.Estado = EstadoDocumento.Error;
            documento.ErrorMensaje = ex.Message;
            await _documentoRepo.UpdateAsync(documento);

            throw;
        }
    }

    public async Task<object> ReemplazarDocumentoAsync(
        int expedienteId, int documentoId,
        Stream documentStream, string fileName,
        IProgress<string>? progreso = null)
    {
        var docExistente = await _documentoRepo.GetByIdAsync(documentoId)
            ?? throw new KeyNotFoundException($"Documento {documentoId} no encontrado");

        if (docExistente.ExpedienteId != expedienteId)
            throw new InvalidOperationException("El documento no pertenece al expediente indicado");

        var tipoDocumento = docExistente.TipoDocumento;

        // Eliminar el documento anterior
        await _documentoRepo.DeleteAsync(documentoId);

        // Procesar el nuevo documento
        return await ProcesarDocumentoAsync(expedienteId, tipoDocumento.Codigo, documentStream, fileName, progreso);
    }

    public async Task<ExpedienteDto> EvaluarExpedienteAsync(int expedienteId)
    {
        var expediente = await _expedienteRepo.GetByIdWithDocumentosAsync(expedienteId)
            ?? throw new KeyNotFoundException($"Expediente {expedienteId} no encontrado");

        // Verificar que todos los obligatorios estén completos
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

        // Extraer datos para evaluación
        var datosEvaluacion = ExtraerDatosParaEvaluacion(expediente);

        // Ejecutar motor de reglas
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

        // Si ya existe un resultado anterior, eliminarlo primero
        if (expediente.ResultadoEvaluacion != null)
        {
            // Actualizar el existente
            expediente.ResultadoEvaluacion.ScoreFinal = resultadoPersistido.ScoreFinal;
            expediente.ResultadoEvaluacion.Recomendacion = resultadoPersistido.Recomendacion;
            expediente.ResultadoEvaluacion.NivelRiesgo = resultadoPersistido.NivelRiesgo;
            expediente.ResultadoEvaluacion.ResultadoCompletoJson = resultadoPersistido.ResultadoCompletoJson;
            expediente.ResultadoEvaluacion.FechaEvaluacion = resultadoPersistido.FechaEvaluacion;
        }
        else
        {
            expediente.Documentos.Clear(); // Necesario para evitar tracking issues
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

    // Métodos privados

    private async Task<object> ProcesarSegunTipoAsync(
        string codigoTipo, Stream documentStream, string fileName,
        IProgress<string>? progreso)
    {
        return codigoTipo switch
        {
            "DNI" => await _documentIntelligence.ProcesarDocumentoIdentidadAsync(documentStream, fileName),
            "VIGENCIA_PODER" => await _documentIntelligence.ProcesarVigenciaPoderAsync(documentStream, fileName, default, progreso),
            "BALANCE_GENERAL" => await _documentIntelligence.ProcesarBalanceGeneralAsync(documentStream, fileName, default, progreso),
            "ESTADO_RESULTADOS" => await _documentIntelligence.ProcesarEstadoResultadosAsync(documentStream, fileName, default, progreso),
            _ => throw new NotSupportedException($"Tipo de documento '{codigoTipo}' no tiene procesamiento implementado")
        };
    }

    private async Task ActualizarDatosExpedienteAsync(Expediente expediente, string codigoTipo, object resultado)
    {
        bool actualizado = false;

        // Auto-llenar datos del solicitante desde el DNI
        if (codigoTipo == "DNI" && resultado is DocumentoIdentidadDto dni)
        {
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

        // Auto-llenar razón social desde Vigencia de Poder
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

        // Auto-llenar desde Balance General
        if (codigoTipo == "BALANCE_GENERAL" && resultado is BalanceGeneralDto balance)
        {
            if (string.IsNullOrEmpty(expediente.RazonSocialEmpresa) && !string.IsNullOrEmpty(balance.RazonSocial))
            {
                expediente.RazonSocialEmpresa = balance.RazonSocial;
                actualizado = true;
            }
        }

        // Auto-llenar desde Estado de Resultados
        if (codigoTipo == "ESTADO_RESULTADOS" && resultado is EstadoResultadosDto estadoRes)
        {
            if (string.IsNullOrEmpty(expediente.RazonSocialEmpresa) && !string.IsNullOrEmpty(estadoRes.RazonSocial))
            {
                expediente.RazonSocialEmpresa = estadoRes.RazonSocial;
                actualizado = true;
            }
        }

        if (actualizado)
        {
            await _expedienteRepo.UpdateAsync(expediente);
        }
    }

    private async Task ActualizarEstadoExpedienteAsync(int expedienteId)
    {
        var expediente = await _expedienteRepo.GetByIdWithDocumentosAsync(expedienteId);
        if (expediente == null) return;

        var tiposObligatorios = await _tipoDocumentoRepo.GetObligatoriosAsync();
        var docsProcesados = expediente.Documentos
            .Where(d => d.Estado == EstadoDocumento.Procesado)
            .Select(d => d.TipoDocumentoId)
            .ToHashSet();

        var todosCompletos = tiposObligatorios.All(t => docsProcesados.Contains(t.Id));

        var nuevoEstado = todosCompletos
            ? EstadoExpediente.DocumentosCompletos
            : EstadoExpediente.EnProceso;

        if (expediente.Estado != EstadoExpediente.Evaluado && expediente.Estado != nuevoEstado)
        {
            expediente.Estado = nuevoEstado;
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
                                datos["MargenBruto"] = estado.MargenBruto.Value / 100; // Ratio, no porcentaje
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

        // Valores por defecto para campos que no se encontraron
        datos.TryAdd("ScoreCrediticio", 500m); // Default medio
        datos.TryAdd("DeudaVencida", 0m); // Sin deuda por defecto

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

        var docsProcesados = expediente.Documentos
            .Where(d => d.Estado == EstadoDocumento.Procesado)
            .Select(d => d.TipoDocumentoId)
            .ToHashSet();

        var obligatoriosCompletos = tiposObligatorios.Count(t => docsProcesados.Contains(t.Id));

        var dto = new ExpedienteDto
        {
            Id = expediente.Id,
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

        // Mapear documentos procesados
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

        // Mapear tipos de documento requeridos
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

        // Mapear resultado de evaluación si existe
        if (expediente.ResultadoEvaluacion != null)
        {
            var resultado = expediente.ResultadoEvaluacion;

            // Intentar deserializar el resultado completo
            ResultadoMotorReglas? resultadoCompleto = null;
            try
            {
                resultadoCompleto = JsonSerializer.Deserialize<ResultadoMotorReglas>(
                    resultado.ResultadoCompletoJson, JsonOptions);
            }
            catch (JsonException)
            {
                // Si no se puede deserializar, dejar los datos básicos
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
}