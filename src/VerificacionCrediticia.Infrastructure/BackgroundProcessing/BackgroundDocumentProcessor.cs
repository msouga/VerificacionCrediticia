using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VerificacionCrediticia.Core.DTOs;
using VerificacionCrediticia.Core.Enums;
using VerificacionCrediticia.Core.Interfaces;

namespace VerificacionCrediticia.Infrastructure.BackgroundProcessing;

public class BackgroundDocumentProcessor : BackgroundService
{
    private readonly IDocumentProcessingQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackgroundDocumentProcessor> _logger;
    private readonly SemaphoreSlim _concurrencyLimiter = new(3, 3);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public BackgroundDocumentProcessor(
        IDocumentProcessingQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<BackgroundDocumentProcessor> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BackgroundDocumentProcessor iniciado");

        // Recovery: re-encolar documentos en Subido o Procesando (app se reinicio)
        await RecoverPendingDocumentsAsync(stoppingToken);

        // Loop principal: leer mensajes del canal
        await foreach (var message in _queue.ReadAllAsync(stoppingToken))
        {
            await _concurrencyLimiter.WaitAsync(stoppingToken);
            _ = ProcesarDocumentoConLimiteAsync(message, stoppingToken);
        }
    }

    private async Task ProcesarDocumentoConLimiteAsync(DocumentoProcesarMessage message, CancellationToken ct)
    {
        try
        {
            await ProcesarDocumentoAsync(message, ct);
        }
        finally
        {
            _concurrencyLimiter.Release();
        }
    }

    private async Task RecoverPendingDocumentsAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var documentoRepo = scope.ServiceProvider.GetRequiredService<IDocumentoProcesadoRepository>();

            var pendientes = await documentoRepo.GetByEstadosAsync(
                [EstadoDocumento.Subido, EstadoDocumento.Procesando], ct);

            if (pendientes.Count > 0)
            {
                _logger.LogInformation("Recovery: re-encolando {Count} documento(s) pendientes", pendientes.Count);

                foreach (var doc in pendientes)
                {
                    if (string.IsNullOrEmpty(doc.BlobUri)) continue;

                    await _queue.EnqueueAsync(new DocumentoProcesarMessage(
                        doc.ExpedienteId,
                        doc.Id,
                        doc.TipoDocumento?.Codigo ?? "",
                        doc.BlobUri,
                        doc.NombreArchivo), ct);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en recovery de documentos pendientes");
        }
    }

    private async Task ProcesarDocumentoAsync(DocumentoProcesarMessage message, CancellationToken ct)
    {
        _logger.LogInformation(
            "Procesando documento {DocId} ({Tipo}) del expediente {ExpId} en background",
            message.DocumentoId, message.CodigoTipo, message.ExpedienteId);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var documentoRepo = scope.ServiceProvider.GetRequiredService<IDocumentoProcesadoRepository>();
            var expedienteRepo = scope.ServiceProvider.GetRequiredService<IExpedienteRepository>();
            var blobStorage = scope.ServiceProvider.GetRequiredService<IBlobStorageService>();
            var processingService = scope.ServiceProvider.GetRequiredService<IDocumentProcessingService>();

            // Verificar que el documento aun existe y esta en estado valido
            var doc = await documentoRepo.GetByIdAsync(message.DocumentoId, ct);
            if (doc == null)
            {
                _logger.LogWarning("Documento {DocId} ya no existe, saltando", message.DocumentoId);
                return;
            }

            if (doc.Estado != EstadoDocumento.Subido && doc.Estado != EstadoDocumento.Procesando)
            {
                _logger.LogInformation("Documento {DocId} ya en estado {Estado}, saltando",
                    message.DocumentoId, doc.Estado);
                return;
            }

            // Actualizar a Procesando
            doc.Estado = EstadoDocumento.Procesando;
            await documentoRepo.UpdateAsync(doc, ct);

            // Descargar del blob
            using var stream = await blobStorage.DownloadAsync(message.BlobUri);

            // Procesar con Content Understanding (con retry)
            object resultado;
            try
            {
                resultado = await processingService.ProcesarSegunTipoAsync(
                    message.CodigoTipo, stream, message.NombreArchivo, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Primer intento fallido para doc {DocId}, reintentando...", message.DocumentoId);

                if (stream.CanSeek) stream.Position = 0;

                resultado = await processingService.ProcesarSegunTipoAsync(
                    message.CodigoTipo, stream, message.NombreArchivo, ct);
            }

            // Guardar resultado
            doc.DatosExtraidosJson = JsonSerializer.Serialize(resultado, JsonOptions);
            doc.Estado = EstadoDocumento.Procesado;
            doc.ConfianzaPromedio = processingService.ObtenerConfianzaDeResultado(resultado);
            doc.ErrorMensaje = null;
            await documentoRepo.UpdateAsync(doc, ct);

            // Actualizar datos del expediente (DNI -> nombre, etc.)
            var expediente = await expedienteRepo.GetByIdTrackingAsync(message.ExpedienteId);
            if (expediente != null)
            {
                await processingService.ActualizarDatosExpedienteAsync(
                    expediente, message.CodigoTipo, resultado);
            }

            _logger.LogInformation(
                "Documento {DocId} ({Tipo}) procesado exitosamente en background. Confianza: {Confianza:P0}",
                message.DocumentoId, message.CodigoTipo, doc.ConfianzaPromedio);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogInformation("Procesamiento cancelado para documento {DocId}", message.DocumentoId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando documento {DocId} ({Tipo}) en background",
                message.DocumentoId, message.CodigoTipo);

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var documentoRepo = scope.ServiceProvider.GetRequiredService<IDocumentoProcesadoRepository>();
                var doc = await documentoRepo.GetByIdAsync(message.DocumentoId);
                if (doc != null)
                {
                    doc.Estado = EstadoDocumento.Error;
                    doc.ErrorMensaje = ex.Message;
                    await documentoRepo.UpdateAsync(doc);
                }
            }
            catch (Exception innerEx)
            {
                _logger.LogError(innerEx, "Error actualizando estado de error para documento {DocId}", message.DocumentoId);
            }
        }
    }
}
