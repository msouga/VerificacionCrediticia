using PDFtoImage;
using SkiaSharp;

namespace VerificacionCrediticia.Infrastructure.AzureOpenAI;

public static class PdfToImageConverter
{
    public static List<string> ConvertToBase64Images(
        byte[] pdfBytes,
        int dpi = 200,
        int maxPages = 10,
        IProgress<string>? progreso = null)
    {
        var images = new List<string>();
        var pageCount = Conversion.GetPageCount(pdfBytes);
        var pagesToProcess = Math.Min(pageCount, maxPages);

        progreso?.Report($"Convirtiendo {pagesToProcess} pagina(s) a imagen...");

        var options = new RenderOptions { Dpi = dpi };

        for (int i = 0; i < pagesToProcess; i++)
        {
            progreso?.Report($"Procesando pagina {i + 1} de {pagesToProcess}...");

            using var bitmap = Conversion.ToImage(pdfBytes, (Index)i, password: null, options: options);
            using var data = bitmap.Encode(SKEncodedImageFormat.Png, 90);
            var base64 = Convert.ToBase64String(data.ToArray());
            images.Add(base64);
        }

        return images;
    }
}
