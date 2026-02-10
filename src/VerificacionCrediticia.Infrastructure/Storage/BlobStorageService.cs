using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VerificacionCrediticia.Core.Interfaces;

namespace VerificacionCrediticia.Infrastructure.Storage;

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(IConfiguration configuration, ILogger<BlobStorageService> logger)
    {
        _logger = logger;
        var connectionString = configuration["AzureStorage:ConnectionString"]
            ?? throw new InvalidOperationException("AzureStorage:ConnectionString no configurada");

        var containerName = configuration["AzureStorage:ContainerName"] ?? "documentos";
        var serviceClient = new BlobServiceClient(connectionString);
        _containerClient = serviceClient.GetBlobContainerClient(containerName);
        _containerClient.CreateIfNotExists();
    }

    public async Task<string> UploadAsync(string blobPath, Stream content, string contentType)
    {
        var blobClient = _containerClient.GetBlobClient(blobPath);

        var options = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        };

        await blobClient.UploadAsync(content, options);
        _logger.LogInformation("Blob subido: {BlobPath}", blobPath);

        return blobClient.Uri.ToString();
    }

    public async Task<Stream> DownloadAsync(string blobUri)
    {
        var blobClient = GetBlobClientFromUri(blobUri);
        var response = await blobClient.DownloadStreamingAsync();
        return response.Value.Content;
    }

    public async Task DeleteAsync(string blobUri)
    {
        var blobClient = GetBlobClientFromUri(blobUri);
        await blobClient.DeleteIfExistsAsync();
        _logger.LogInformation("Blob eliminado: {BlobUri}", blobUri);
    }

    private BlobClient GetBlobClientFromUri(string blobUri)
    {
        // Extraer el nombre del blob de la URI completa
        var uri = new Uri(blobUri);
        var containerPrefix = $"/{_containerClient.Name}/";
        var blobName = uri.AbsolutePath;

        var containerIndex = blobName.IndexOf(containerPrefix, StringComparison.OrdinalIgnoreCase);
        if (containerIndex >= 0)
        {
            blobName = blobName.Substring(containerIndex + containerPrefix.Length);
        }

        return _containerClient.GetBlobClient(blobName);
    }
}
