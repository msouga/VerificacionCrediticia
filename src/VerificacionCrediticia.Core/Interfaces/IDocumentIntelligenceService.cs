using VerificacionCrediticia.Core.DTOs;

namespace VerificacionCrediticia.Core.Interfaces;

/// <summary>
/// Servicio para procesar documentos usando Azure AI Document Intelligence
/// </summary>
public interface IDocumentIntelligenceService
{
    /// <summary>
    /// Procesa un documento de identidad (DNI) y extrae sus datos
    /// </summary>
    /// <param name="documentStream">Stream del archivo PDF o imagen</param>
    /// <param name="nombreArchivo">Nombre del archivo original</param>
    /// <param name="cancellationToken">Token de cancelacion</param>
    /// <returns>Datos extraidos del documento de identidad</returns>
    Task<DocumentoIdentidadDto> ProcesarDocumentoIdentidadAsync(
        Stream documentStream,
        string nombreArchivo,
        CancellationToken cancellationToken = default);
}
