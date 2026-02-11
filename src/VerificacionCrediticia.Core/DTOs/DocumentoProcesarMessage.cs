namespace VerificacionCrediticia.Core.DTOs;

public record DocumentoProcesarMessage(
    int ExpedienteId,
    int DocumentoId,
    string CodigoTipo,
    string BlobUri,
    string NombreArchivo);
