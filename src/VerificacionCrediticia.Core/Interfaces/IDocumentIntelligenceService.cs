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

    /// <summary>
    /// Procesa una Vigencia de Poder y extrae datos de empresa y representantes
    /// </summary>
    Task<VigenciaPoderDto> ProcesarVigenciaPoderAsync(
        Stream documentStream,
        string nombreArchivo,
        CancellationToken cancellationToken = default,
        IProgress<string>? progreso = null);

    /// <summary>
    /// Procesa un Balance General y extrae partidas contables y firmantes
    /// </summary>
    Task<BalanceGeneralDto> ProcesarBalanceGeneralAsync(
        Stream documentStream,
        string nombreArchivo,
        CancellationToken cancellationToken = default,
        IProgress<string>? progreso = null);

    /// <summary>
    /// Procesa un Estado de Resultados y extrae partidas financieras
    /// </summary>
    Task<EstadoResultadosDto> ProcesarEstadoResultadosAsync(
        Stream documentStream,
        string nombreArchivo,
        CancellationToken cancellationToken = default,
        IProgress<string>? progreso = null);

    /// <summary>
    /// Procesa una Ficha RUC de SUNAT y extrae datos del contribuyente
    /// </summary>
    Task<FichaRucDto> ProcesarFichaRucAsync(
        Stream documentStream,
        string nombreArchivo,
        CancellationToken cancellationToken = default,
        IProgress<string>? progreso = null);

    /// <summary>
    /// Clasifica un documento usando Content Understanding contentCategories
    /// y extrae campos automaticamente si el sub-analyzer esta enlazado.
    /// Retorna la categoria detectada y el resultado de extraccion tipado.
    /// </summary>
    Task<ClasificacionResultadoDto> ClasificarYProcesarAsync(
        Stream documentStream,
        string nombreArchivo,
        CancellationToken cancellationToken = default,
        IProgress<string>? progreso = null);
}
