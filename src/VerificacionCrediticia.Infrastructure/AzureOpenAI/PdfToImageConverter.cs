using PDFtoImage;
using SkiaSharp;

namespace VerificacionCrediticia.Infrastructure.AzureOpenAI;

public static class PdfToImageConverter
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".bmp", ".tiff", ".tif", ".gif", ".webp"
    };

    public static List<string> ConvertToBase64Images(
        byte[] fileBytes,
        int dpi = 200,
        int maxPages = 10,
        IProgress<string>? progreso = null,
        string? nombreArchivo = null)
    {
        // Si es una imagen (no PDF), convertir directo a base64
        if (nombreArchivo != null && EsImagen(nombreArchivo))
        {
            progreso?.Report("Procesando imagen...");
            return [ConvertImageToBase64Png(fileBytes)];
        }

        // PDF: convertir paginas a imagenes
        var images = new List<string>();
        var pageCount = Conversion.GetPageCount(fileBytes);
        var pagesToProcess = Math.Min(pageCount, maxPages);

        progreso?.Report($"Convirtiendo {pagesToProcess} pagina(s) a imagen...");

        var options = new RenderOptions { Dpi = dpi };

        for (int i = 0; i < pagesToProcess; i++)
        {
            progreso?.Report($"Procesando pagina {i + 1} de {pagesToProcess}...");

            using var bitmap = Conversion.ToImage(fileBytes, (Index)i, password: null, options: options);
            using var data = bitmap.Encode(SKEncodedImageFormat.Png, 90);
            var base64 = Convert.ToBase64String(data.ToArray());
            images.Add(base64);
        }

        return images;
    }

    private static bool EsImagen(string nombreArchivo)
    {
        var extension = Path.GetExtension(nombreArchivo);
        return !string.IsNullOrEmpty(extension) && ImageExtensions.Contains(extension);
    }

    private static string ConvertImageToBase64Png(byte[] imageBytes)
    {
        using var bitmap = SKBitmap.Decode(imageBytes);
        if (bitmap == null)
            throw new InvalidOperationException("No se pudo decodificar la imagen");

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 90);
        return Convert.ToBase64String(data.ToArray());
    }
}
