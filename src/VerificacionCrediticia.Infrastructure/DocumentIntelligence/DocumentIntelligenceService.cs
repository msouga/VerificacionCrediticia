using System.Text.RegularExpressions;
using Azure;
using Azure.AI.DocumentIntelligence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VerificacionCrediticia.Core.DTOs;
using VerificacionCrediticia.Core.Interfaces;

namespace VerificacionCrediticia.Infrastructure.DocumentIntelligence;

public class DocumentIntelligenceService : IDocumentIntelligenceService
{
    private readonly DocumentIntelligenceClient _client;
    private readonly ILogger<DocumentIntelligenceService> _logger;

    public DocumentIntelligenceService(
        IOptions<DocumentIntelligenceSettings> settings,
        ILogger<DocumentIntelligenceService> logger)
    {
        _logger = logger;

        var config = settings.Value;
        var credential = new AzureKeyCredential(config.ApiKey);
        _client = new DocumentIntelligenceClient(new Uri(config.Endpoint), credential);
    }

    public async Task<DocumentoIdentidadDto> ProcesarDocumentoIdentidadAsync(
        Stream documentStream,
        string nombreArchivo,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Procesando documento de identidad: {NombreArchivo}", nombreArchivo);

        using var memoryStream = new MemoryStream();
        await documentStream.CopyToAsync(memoryStream, cancellationToken);
        var bytes = memoryStream.ToArray();
        var bytesSource = BinaryData.FromBytes(bytes);

        // Paso 1: prebuilt-idDocument para extraccion estructurada
        var idOperation = await _client.AnalyzeDocumentAsync(
            WaitUntil.Completed,
            "prebuilt-idDocument",
            bytesSource,
            cancellationToken);

        var idResult = idOperation.Value;

        var dto = new DocumentoIdentidadDto { ArchivoOrigen = nombreArchivo };

        if (idResult.Documents != null && idResult.Documents.Count > 0)
        {
            var documento = idResult.Documents[0];

            dto.TipoDocumento = ExtraerTexto(documento, "DocumentType");
            dto.Nombres = ExtraerTexto(documento, "FirstName");
            RegistrarConfianza(dto, "Nombres", documento, "FirstName");
            dto.Apellidos = ExtraerTexto(documento, "LastName");
            RegistrarConfianza(dto, "Apellidos", documento, "LastName");
            dto.NumeroDocumento = ExtraerTexto(documento, "DocumentNumber");
            RegistrarConfianza(dto, "NumeroDocumento", documento, "DocumentNumber");
            dto.FechaNacimiento = ExtraerFecha(documento, "DateOfBirth");
            RegistrarConfianza(dto, "FechaNacimiento", documento, "DateOfBirth");
            dto.FechaExpiracion = ExtraerFecha(documento, "DateOfExpiration");
            RegistrarConfianza(dto, "FechaExpiracion", documento, "DateOfExpiration");
            dto.Sexo = ExtraerTexto(documento, "Sex");
            RegistrarConfianza(dto, "Sexo", documento, "Sex");
            dto.Direccion = ExtraerTexto(documento, "Address");
            RegistrarConfianza(dto, "Direccion", documento, "Address");
            dto.Nacionalidad = ExtraerTexto(documento, "CountryRegion");
            RegistrarConfianza(dto, "Nacionalidad", documento, "CountryRegion");
        }

        // Post-procesamiento: limpiar numero de documento peruano (8 digitos)
        dto.NumeroDocumento = LimpiarNumeroDocumentoPeru(dto.NumeroDocumento);

        // Paso 2: Si faltan campos clave, hacer segundo pase con prebuilt-read
        if (string.IsNullOrEmpty(dto.Sexo) || string.IsNullOrEmpty(dto.EstadoCivil))
        {
            _logger.LogInformation("Campos faltantes detectados, ejecutando segundo pase con prebuilt-read");

            var readOperation = await _client.AnalyzeDocumentAsync(
                WaitUntil.Completed,
                "prebuilt-read",
                BinaryData.FromBytes(bytes),
                cancellationToken);

            var readResult = readOperation.Value;
            var lineas = ExtraerLineas(readResult);

            if (string.IsNullOrEmpty(dto.Sexo))
            {
                dto.Sexo = BuscarSexo(lineas);
                if (!string.IsNullOrEmpty(dto.Sexo))
                {
                    dto.Confianza["Sexo"] = 0.85f; // Confianza estimada por layout
                    _logger.LogInformation("Sexo extraido por layout: {Sexo}", dto.Sexo);
                }
            }

            if (string.IsNullOrEmpty(dto.EstadoCivil))
            {
                dto.EstadoCivil = BuscarEstadoCivil(lineas);
                if (!string.IsNullOrEmpty(dto.EstadoCivil))
                {
                    dto.Confianza["EstadoCivil"] = 0.85f;
                    _logger.LogInformation("Estado civil extraido por layout: {EstadoCivil}", dto.EstadoCivil);
                }
            }
        }

        // Calcular confianza promedio
        if (dto.Confianza.Count > 0)
        {
            dto.ConfianzaPromedio = dto.Confianza.Values.Average();
        }

        _logger.LogInformation(
            "Documento procesado: DNI {Dni}, Nombre: {Nombre} {Apellido}, Sexo: {Sexo}, Confianza promedio: {Confianza:P1}",
            dto.NumeroDocumento,
            dto.Nombres,
            dto.Apellidos,
            dto.Sexo,
            dto.ConfianzaPromedio);

        return dto;
    }

    /// <summary>
    /// Limpia el numero de documento peruano: extrae solo los 8 digitos del DNI
    /// </summary>
    private static string? LimpiarNumeroDocumentoPeru(string? numero)
    {
        if (string.IsNullOrEmpty(numero))
            return numero;

        // Extraer solo digitos
        var soloDigitos = Regex.Replace(numero, @"[^\d]", "");

        // DNI peruano tiene 8 digitos; si hay 9, el ultimo es digito verificador
        if (soloDigitos.Length >= 8)
            return soloDigitos[..8];

        return soloDigitos;
    }

    /// <summary>
    /// Extrae todas las lineas de texto del resultado de prebuilt-read, ordenadas por posicion vertical
    /// </summary>
    private static List<LineaTexto> ExtraerLineas(AnalyzeResult result)
    {
        var lineas = new List<LineaTexto>();

        if (result.Pages == null)
            return lineas;

        foreach (var page in result.Pages)
        {
            if (page.Lines == null) continue;

            foreach (var line in page.Lines)
            {
                // Usar el primer punto del poligono como posicion Y aproximada
                float y = 0;
                if (line.Polygon != null && line.Polygon.Count >= 2)
                {
                    y = line.Polygon[1]; // Y del primer punto
                }

                lineas.Add(new LineaTexto
                {
                    Texto = line.Content?.Trim() ?? "",
                    PosicionY = y
                });
            }
        }

        return lineas.OrderBy(l => l.PosicionY).ToList();
    }

    /// <summary>
    /// Busca el valor de Sexo en las lineas del OCR usando proximidad vertical.
    /// El DNI peruano tiene "Sexo" como etiqueta y debajo "F" o "M".
    /// Valida contra valores conocidos para evitar falsos positivos.
    /// </summary>
    private static string? BuscarSexo(List<LineaTexto> lineas)
    {
        var valoresValidos = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "F", "M", "FEMENINO", "MASCULINO"
        };

        for (int i = 0; i < lineas.Count; i++)
        {
            if (!lineas[i].Texto.Contains("SEXO", StringComparison.OrdinalIgnoreCase))
                continue;

            var yEtiqueta = lineas[i].PosicionY;

            // Intentar extraer de la misma linea (ej: "Sexo M")
            var idx = lineas[i].Texto.IndexOf("SEXO", StringComparison.OrdinalIgnoreCase);
            var despues = lineas[i].Texto[(idx + 4)..].Trim();
            var tokens = despues.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                if (valoresValidos.Contains(token))
                    return token.ToUpperInvariant();
            }

            // Buscar en lineas cercanas por posicion Y (delta < 1.0)
            for (int j = i + 1; j < lineas.Count; j++)
            {
                if (lineas[j].PosicionY - yEtiqueta > 1.0f) break;

                var lineaSiguiente = lineas[j].Texto.Trim();

                if (valoresValidos.Contains(lineaSiguiente))
                    return lineaSiguiente.ToUpperInvariant();

                var primerToken = lineaSiguiente.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault();
                if (primerToken != null && valoresValidos.Contains(primerToken))
                    return primerToken.ToUpperInvariant();
            }
        }

        return null;
    }

    /// <summary>
    /// Busca el estado civil en las lineas del OCR usando proximidad vertical.
    /// El DNI peruano tiene "Estado Civil" como etiqueta y debajo "S", "C", "V", "D".
    /// </summary>
    private static string? BuscarEstadoCivil(List<LineaTexto> lineas)
    {
        var valoresValidos = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "S", "C", "V", "D",
            "SOLTERO", "SOLTERA", "CASADO", "CASADA",
            "VIUDO", "VIUDA", "DIVORCIADO", "DIVORCIADA"
        };

        for (int i = 0; i < lineas.Count; i++)
        {
            if (!lineas[i].Texto.Contains("ESTADO CIVIL", StringComparison.OrdinalIgnoreCase) &&
                !lineas[i].Texto.Contains("ESTADO\tCIVIL", StringComparison.OrdinalIgnoreCase))
                continue;

            var yEtiqueta = lineas[i].PosicionY;

            // Intentar extraer de la misma linea
            var idx = lineas[i].Texto.IndexOf("CIVIL", StringComparison.OrdinalIgnoreCase);
            var despues = lineas[i].Texto[(idx + 5)..].Trim();
            var tokens = despues.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                if (valoresValidos.Contains(token))
                    return token.ToUpperInvariant();
            }

            // Buscar en lineas cercanas por posicion Y (delta < 1.0)
            for (int j = i + 1; j < lineas.Count; j++)
            {
                if (lineas[j].PosicionY - yEtiqueta > 1.0f) break;

                var lineaSiguiente = lineas[j].Texto.Trim();

                if (valoresValidos.Contains(lineaSiguiente))
                    return lineaSiguiente.ToUpperInvariant();

                // Ultimo token de la linea (ej: "M S" -> "S" es el estado civil)
                var ultimoToken = lineaSiguiente.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                    .LastOrDefault();
                if (ultimoToken != null && valoresValidos.Contains(ultimoToken))
                    return ultimoToken.ToUpperInvariant();
            }
        }

        return null;
    }

    private static string? ExtraerTexto(AnalyzedDocument documento, string fieldName)
    {
        if (documento.Fields.TryGetValue(fieldName, out var field))
        {
            if (field.FieldType == DocumentFieldType.String)
                return field.ValueString;

            if (field.FieldType == DocumentFieldType.CountryRegion)
                return field.ValueCountryRegion;

            if (field.Content != null)
                return field.Content;
        }

        return null;
    }

    private static string? ExtraerFecha(AnalyzedDocument documento, string fieldName)
    {
        if (documento.Fields.TryGetValue(fieldName, out var field) &&
            field.FieldType == DocumentFieldType.Date &&
            field.ValueDate.HasValue)
        {
            return field.ValueDate.Value.ToString("dd/MM/yyyy");
        }

        return null;
    }

    private static void RegistrarConfianza(
        DocumentoIdentidadDto dto,
        string nombreCampo,
        AnalyzedDocument documento,
        string fieldName)
    {
        if (documento.Fields.TryGetValue(fieldName, out var field) && field.Confidence.HasValue)
        {
            dto.Confianza[nombreCampo] = field.Confidence.Value;
        }
    }

    private class LineaTexto
    {
        public string Texto { get; set; } = "";
        public float PosicionY { get; set; }
    }
}
