namespace VerificacionCrediticia.Core.Interfaces;

public interface IBlobStorageService
{
    Task<string> UploadAsync(string blobPath, Stream content, string contentType);
    Task<Stream> DownloadAsync(string blobUri);
    Task DeleteAsync(string blobUri);
}
