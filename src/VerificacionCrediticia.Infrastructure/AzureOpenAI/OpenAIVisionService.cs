using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VerificacionCrediticia.Core.DTOs;
using VerificacionCrediticia.Core.Interfaces;

namespace VerificacionCrediticia.Infrastructure.AzureOpenAI;

public class OpenAIVisionService : IDocumentIntelligenceService
{
    private readonly HttpClient _httpClient;
    private readonly AzureOpenAISettings _settings;
    private readonly ILogger<OpenAIVisionService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public OpenAIVisionService(
        HttpClient httpClient,
        IOptions<AzureOpenAISettings> settings,
        ILogger<OpenAIVisionService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<ClasificacionResultadoDto> ClasificarYProcesarAsync(
        Stream documentStream,
        string nombreArchivo,
        CancellationToken cancellationToken = default,
        IProgress<string>? progreso = null)
    {
        _logger.LogInformation("Clasificando y procesando documento con GPT-4.1 Vision: {NombreArchivo}", nombreArchivo);

        var respuesta = await EnviarAVisionAsync(documentStream, nombreArchivo, DocumentPrompts.GetUniversalPrompt(), cancellationToken, progreso);

        var tipo = respuesta.GetProperty("tipo").GetString() ?? "OTHER";
        var confianzaClasificacion = ObtenerDecimal(respuesta, "confianza_clasificacion") ?? 0m;

        _logger.LogInformation("Documento clasificado como: {Tipo}, confianza: {Confianza:P1}", tipo, confianzaClasificacion);

        object? resultadoExtraccion = null;
        if (respuesta.TryGetProperty("datos", out var datos) && datos.ValueKind != JsonValueKind.Null)
        {
            var confianzaCampos = ObtenerConfianzaCampos(respuesta);

            resultadoExtraccion = tipo switch
            {
                "DNI" => MapearDni(datos, confianzaCampos, nombreArchivo),
                "VIGENCIA_PODER" => MapearVigenciaPoder(datos, confianzaCampos, nombreArchivo),
                "BALANCE_GENERAL" => MapearBalanceGeneral(datos, confianzaCampos, nombreArchivo),
                "ESTADO_RESULTADOS" => MapearEstadoResultados(datos, confianzaCampos, nombreArchivo),
                "FICHA_RUC" => MapearFichaRuc(datos, confianzaCampos, nombreArchivo),
                _ => null
            };
        }

        return new ClasificacionResultadoDto
        {
            CategoriaDetectada = tipo,
            ResultadoExtraccion = resultadoExtraccion,
            ConfianzaClasificacion = confianzaClasificacion
        };
    }

    public async Task<DocumentoIdentidadDto> ProcesarDocumentoIdentidadAsync(
        Stream documentStream,
        string nombreArchivo,
        CancellationToken cancellationToken = default)
    {
        var resultado = await ClasificarYProcesarAsync(documentStream, nombreArchivo, cancellationToken);
        if (resultado.ResultadoExtraccion is DocumentoIdentidadDto dto)
            return dto;

        _logger.LogWarning("El documento no fue clasificado como DNI: {Tipo}", resultado.CategoriaDetectada);
        return new DocumentoIdentidadDto { ArchivoOrigen = nombreArchivo };
    }

    public async Task<VigenciaPoderDto> ProcesarVigenciaPoderAsync(
        Stream documentStream,
        string nombreArchivo,
        CancellationToken cancellationToken = default,
        IProgress<string>? progreso = null)
    {
        var resultado = await ClasificarYProcesarAsync(documentStream, nombreArchivo, cancellationToken, progreso);
        if (resultado.ResultadoExtraccion is VigenciaPoderDto dto)
            return dto;

        _logger.LogWarning("El documento no fue clasificado como VIGENCIA_PODER: {Tipo}", resultado.CategoriaDetectada);
        return new VigenciaPoderDto { ArchivoOrigen = nombreArchivo };
    }

    public async Task<BalanceGeneralDto> ProcesarBalanceGeneralAsync(
        Stream documentStream,
        string nombreArchivo,
        CancellationToken cancellationToken = default,
        IProgress<string>? progreso = null)
    {
        var resultado = await ClasificarYProcesarAsync(documentStream, nombreArchivo, cancellationToken, progreso);
        if (resultado.ResultadoExtraccion is BalanceGeneralDto dto)
            return dto;

        _logger.LogWarning("El documento no fue clasificado como BALANCE_GENERAL: {Tipo}", resultado.CategoriaDetectada);
        return new BalanceGeneralDto { ArchivoOrigen = nombreArchivo };
    }

    public async Task<EstadoResultadosDto> ProcesarEstadoResultadosAsync(
        Stream documentStream,
        string nombreArchivo,
        CancellationToken cancellationToken = default,
        IProgress<string>? progreso = null)
    {
        var resultado = await ClasificarYProcesarAsync(documentStream, nombreArchivo, cancellationToken, progreso);
        if (resultado.ResultadoExtraccion is EstadoResultadosDto dto)
            return dto;

        _logger.LogWarning("El documento no fue clasificado como ESTADO_RESULTADOS: {Tipo}", resultado.CategoriaDetectada);
        return new EstadoResultadosDto { FechaProcesado = DateTime.UtcNow };
    }

    public async Task<FichaRucDto> ProcesarFichaRucAsync(
        Stream documentStream,
        string nombreArchivo,
        CancellationToken cancellationToken = default,
        IProgress<string>? progreso = null)
    {
        var resultado = await ClasificarYProcesarAsync(documentStream, nombreArchivo, cancellationToken, progreso);
        if (resultado.ResultadoExtraccion is FichaRucDto dto)
            return dto;

        _logger.LogWarning("El documento no fue clasificado como FICHA_RUC: {Tipo}", resultado.CategoriaDetectada);
        return new FichaRucDto { ArchivoOrigen = nombreArchivo };
    }

    private async Task<JsonElement> EnviarAVisionAsync(
        Stream documentStream,
        string nombreArchivo,
        string systemPrompt,
        CancellationToken cancellationToken,
        IProgress<string>? progreso = null)
    {
        progreso?.Report("Preparando documento para analisis...");

        using var memoryStream = new MemoryStream();
        await documentStream.CopyToAsync(memoryStream, cancellationToken);
        var bytes = memoryStream.ToArray();

        // Convertir documento a imagenes (PDF -> paginas, imagen -> directo)
        var images = PdfToImageConverter.ConvertToBase64Images(
            bytes, _settings.ImageDpi, _settings.MaxPagesPerDocument, progreso, nombreArchivo);

        progreso?.Report("Enviando documento a GPT-4.1 Vision...");

        // Construir los content items del user message
        var userContent = new List<object>
        {
            new { type = "text", text = $"Analiza este documento: {nombreArchivo}" }
        };

        foreach (var base64 in images)
        {
            userContent.Add(new
            {
                type = "image_url",
                image_url = new
                {
                    url = $"data:image/png;base64,{base64}",
                    detail = "high"
                }
            });
        }

        var requestBody = new
        {
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userContent }
            },
            max_tokens = _settings.MaxTokens,
            temperature = _settings.Temperature,
            response_format = new { type = "json_object" }
        };

        var endpoint = _settings.Endpoint.TrimEnd('/');
        var url = $"{endpoint}/openai/deployments/{_settings.DeploymentName}/chat/completions?api-version={_settings.ApiVersion}";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("api-key", _settings.ApiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            System.Text.Encoding.UTF8,
            "application/json");

        progreso?.Report("Analizando documento con inteligencia artificial...");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseJson);

        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrEmpty(content))
            throw new InvalidOperationException("GPT-4.1 Vision retorno contenido vacio");

        // Limpiar posibles bloques de codigo markdown
        content = LimpiarJsonResponse(content);

        progreso?.Report("Procesando resultados...");

        using var resultDoc = JsonDocument.Parse(content);
        // Clonar para que sobreviva al Dispose
        return resultDoc.RootElement.Clone();
    }

    private static string LimpiarJsonResponse(string content)
    {
        content = content.Trim();
        if (content.StartsWith("```"))
        {
            var firstNewline = content.IndexOf('\n');
            if (firstNewline > 0)
                content = content[(firstNewline + 1)..];
            if (content.EndsWith("```"))
                content = content[..^3];
            content = content.Trim();
        }
        return content;
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct = default)
    {
        if (response.IsSuccessStatusCode) return;

        string detalle;
        try
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var errorObj)
                && errorObj.TryGetProperty("message", out var msgProp))
            {
                var code = errorObj.TryGetProperty("code", out var codeProp) ? codeProp.GetString() : null;
                detalle = code != null ? $"{code}: {msgProp.GetString()}" : msgProp.GetString() ?? body;
            }
            else
            {
                detalle = body.Length > 500 ? body[..500] : body;
            }
        }
        catch
        {
            detalle = response.ReasonPhrase ?? "Sin detalle";
        }

        var mensaje = $"Azure OpenAI respondio {(int)response.StatusCode} ({response.StatusCode}): {detalle}";
        _logger.LogError("Error HTTP de Azure OpenAI: {Mensaje}", mensaje);
        throw new HttpRequestException(mensaje, null, response.StatusCode);
    }

    // Metodos de mapeo de JSON a DTOs

    private DocumentoIdentidadDto MapearDni(JsonElement datos, Dictionary<string, float> confianza, string nombreArchivo)
    {
        var dto = new DocumentoIdentidadDto
        {
            ArchivoOrigen = nombreArchivo,
            TipoDocumento = "DNI",
            Nombres = ObtenerString(datos, "Nombres"),
            Apellidos = ObtenerString(datos, "Apellidos"),
            NumeroDocumento = LimpiarNumeroDocumentoPeru(ObtenerString(datos, "NumeroDocumento")),
            FechaNacimiento = ObtenerString(datos, "FechaNacimiento"),
            FechaExpiracion = ObtenerString(datos, "FechaExpiracion"),
            Sexo = ObtenerString(datos, "Sexo"),
            EstadoCivil = ObtenerString(datos, "EstadoCivil"),
            Direccion = ObtenerString(datos, "Direccion"),
            Nacionalidad = "PER",
            Confianza = confianza
        };

        if (confianza.Count > 0)
            dto.ConfianzaPromedio = confianza.Values.Average();

        _logger.LogInformation(
            "DNI procesado: {Dni}, Nombre: {Nombre} {Apellido}, Confianza: {Confianza:P1}",
            dto.NumeroDocumento, dto.Nombres, dto.Apellidos, dto.ConfianzaPromedio);

        return dto;
    }

    private VigenciaPoderDto MapearVigenciaPoder(JsonElement datos, Dictionary<string, float> confianza, string nombreArchivo)
    {
        var dto = new VigenciaPoderDto
        {
            ArchivoOrigen = nombreArchivo,
            Ruc = LimpiarRuc(ObtenerString(datos, "Ruc")),
            RazonSocial = ObtenerString(datos, "RazonSocial"),
            TipoPersonaJuridica = ObtenerString(datos, "TipoPersonaJuridica"),
            Domicilio = ObtenerString(datos, "Domicilio"),
            ObjetoSocial = ObtenerString(datos, "ObjetoSocial"),
            CapitalSocial = ObtenerString(datos, "CapitalSocial"),
            PartidaRegistral = ObtenerString(datos, "PartidaRegistral"),
            FechaConstitucion = ObtenerString(datos, "FechaConstitucion"),
            Confianza = confianza
        };

        if (datos.TryGetProperty("Representantes", out var reps) && reps.ValueKind == JsonValueKind.Array)
        {
            foreach (var rep in reps.EnumerateArray())
            {
                dto.Representantes.Add(new RepresentanteDto
                {
                    Nombre = ObtenerString(rep, "Nombre"),
                    DocumentoIdentidad = LimpiarNumeroDocumentoPeru(ObtenerString(rep, "DocumentoIdentidad")),
                    Cargo = ObtenerString(rep, "Cargo"),
                    FechaNombramiento = ObtenerString(rep, "FechaNombramiento"),
                    Facultades = ObtenerString(rep, "Facultades")
                });
            }
        }

        if (confianza.Count > 0)
            dto.ConfianzaPromedio = confianza.Values.Average();

        _logger.LogInformation(
            "Vigencia de Poder procesada: RUC {Ruc}, Empresa: {Empresa}, Representantes: {Count}, Confianza: {Confianza:P1}",
            dto.Ruc, dto.RazonSocial, dto.Representantes.Count, dto.ConfianzaPromedio);

        return dto;
    }

    private BalanceGeneralDto MapearBalanceGeneral(JsonElement datos, Dictionary<string, float> confianza, string nombreArchivo)
    {
        var dto = new BalanceGeneralDto
        {
            ArchivoOrigen = nombreArchivo,
            Ruc = LimpiarRuc(ObtenerString(datos, "Ruc")),
            RazonSocial = ObtenerString(datos, "RazonSocial"),
            Domicilio = ObtenerString(datos, "Domicilio"),
            FechaBalance = ObtenerString(datos, "FechaBalance"),
            Moneda = ObtenerString(datos, "Moneda"),

            // Activos corrientes
            EfectivoEquivalentes = ObtenerDecimal(datos, "EfectivoEquivalentes"),
            CuentasCobrarComerciales = ObtenerDecimal(datos, "CuentasCobrarComerciales"),
            CuentasCobrarDiversas = ObtenerDecimal(datos, "CuentasCobrarDiversas"),
            Existencias = ObtenerDecimal(datos, "Existencias"),
            GastosPagadosAnticipado = ObtenerDecimal(datos, "GastosPagadosAnticipado"),
            TotalActivoCorriente = ObtenerDecimal(datos, "TotalActivoCorriente"),

            // Activos no corrientes
            InmueblesMaquinariaEquipo = ObtenerDecimal(datos, "InmueblesMaquinariaEquipo"),
            DepreciacionAcumulada = ObtenerDecimal(datos, "DepreciacionAcumulada"),
            Intangibles = ObtenerDecimal(datos, "Intangibles"),
            AmortizacionAcumulada = ObtenerDecimal(datos, "AmortizacionAcumulada"),
            ActivoDiferido = ObtenerDecimal(datos, "ActivoDiferido"),
            TotalActivoNoCorriente = ObtenerDecimal(datos, "TotalActivoNoCorriente"),

            TotalActivo = ObtenerDecimal(datos, "TotalActivo"),

            // Pasivos corrientes
            TributosPorPagar = ObtenerDecimal(datos, "TributosPorPagar"),
            RemuneracionesPorPagar = ObtenerDecimal(datos, "RemuneracionesPorPagar"),
            CuentasPagarComerciales = ObtenerDecimal(datos, "CuentasPagarComerciales"),
            ObligacionesFinancierasCorto = ObtenerDecimal(datos, "ObligacionesFinancierasCorto"),
            OtrasCuentasPorPagar = ObtenerDecimal(datos, "OtrasCuentasPorPagar"),
            TotalPasivoCorriente = ObtenerDecimal(datos, "TotalPasivoCorriente"),

            // Pasivos no corrientes
            ObligacionesFinancierasLargo = ObtenerDecimal(datos, "ObligacionesFinancierasLargo"),
            Provisiones = ObtenerDecimal(datos, "Provisiones"),
            TotalPasivoNoCorriente = ObtenerDecimal(datos, "TotalPasivoNoCorriente"),

            TotalPasivo = ObtenerDecimal(datos, "TotalPasivo"),

            // Patrimonio
            CapitalSocial = ObtenerDecimal(datos, "CapitalSocial"),
            ReservaLegal = ObtenerDecimal(datos, "ReservaLegal"),
            ResultadosAcumulados = ObtenerDecimal(datos, "ResultadosAcumulados"),
            ResultadoEjercicio = ObtenerDecimal(datos, "ResultadoEjercicio"),
            TotalPatrimonio = ObtenerDecimal(datos, "TotalPatrimonio"),

            TotalPasivoPatrimonio = ObtenerDecimal(datos, "TotalPasivoPatrimonio"),

            Confianza = confianza
        };

        // Firmantes
        if (datos.TryGetProperty("Firmantes", out var firmantes) && firmantes.ValueKind == JsonValueKind.Array)
        {
            foreach (var f in firmantes.EnumerateArray())
            {
                dto.Firmantes.Add(new FirmanteDto
                {
                    Nombre = ObtenerString(f, "Nombre"),
                    Dni = LimpiarNumeroDocumentoPeru(ObtenerString(f, "Dni")),
                    Cargo = ObtenerString(f, "Cargo"),
                    Matricula = ObtenerString(f, "Matricula")
                });
            }
        }

        if (confianza.Count > 0)
            dto.ConfianzaPromedio = confianza.Values.Average();

        // Calcular ratios financieros
        CalcularRatiosFinancieros(dto);

        _logger.LogInformation(
            "Balance General procesado: RUC {Ruc}, Empresa: {Empresa}, Total Activo: {TotalActivo}, Confianza: {Confianza:P1}",
            dto.Ruc, dto.RazonSocial, dto.TotalActivo, dto.ConfianzaPromedio);

        return dto;
    }

    private EstadoResultadosDto MapearEstadoResultados(JsonElement datos, Dictionary<string, float> confianza, string nombreArchivo)
    {
        var dto = new EstadoResultadosDto
        {
            FechaProcesado = DateTime.UtcNow,
            Ruc = LimpiarRuc(ObtenerString(datos, "Ruc")),
            RazonSocial = ObtenerString(datos, "RazonSocial"),
            Periodo = ObtenerString(datos, "Periodo"),
            Moneda = ObtenerString(datos, "Moneda"),

            VentasNetas = ObtenerDecimal(datos, "VentasNetas"),
            CostoVentas = ObtenerDecimal(datos, "CostoVentas"),
            UtilidadBruta = ObtenerDecimal(datos, "UtilidadBruta"),
            GastosAdministrativos = ObtenerDecimal(datos, "GastosAdministrativos"),
            GastosVentas = ObtenerDecimal(datos, "GastosVentas"),
            UtilidadOperativa = ObtenerDecimal(datos, "UtilidadOperativa"),
            OtrosIngresos = ObtenerDecimal(datos, "OtrosIngresos"),
            OtrosGastos = ObtenerDecimal(datos, "OtrosGastos"),
            UtilidadAntesImpuestos = ObtenerDecimal(datos, "UtilidadAntesImpuestos"),
            ImpuestoRenta = ObtenerDecimal(datos, "ImpuestoRenta"),
            UtilidadNeta = ObtenerDecimal(datos, "UtilidadNeta"),

            DatosValidosRuc = !string.IsNullOrEmpty(LimpiarRuc(ObtenerString(datos, "Ruc")))
                               && LimpiarRuc(ObtenerString(datos, "Ruc"))!.Length == 11
        };

        dto.CalcularRatios();

        if (confianza.Count > 0)
            dto.ConfianzaPromedio = (decimal)confianza.Values.Average();

        _logger.LogInformation(
            "Estado de Resultados procesado: RUC {Ruc}, Ventas: {Ventas}, Utilidad Neta: {UtilidadNeta}, Confianza: {Confianza:P1}",
            dto.Ruc, dto.VentasNetas, dto.UtilidadNeta, dto.ConfianzaPromedio);

        return dto;
    }

    private FichaRucDto MapearFichaRuc(JsonElement datos, Dictionary<string, float> confianza, string nombreArchivo)
    {
        var dto = new FichaRucDto
        {
            ArchivoOrigen = nombreArchivo,
            Ruc = LimpiarRuc(ObtenerString(datos, "Ruc")),
            RazonSocial = ObtenerString(datos, "RazonSocial"),
            NombreComercial = ObtenerString(datos, "NombreComercial"),
            TipoContribuyente = ObtenerString(datos, "TipoContribuyente"),
            FechaInscripcion = ObtenerString(datos, "FechaInscripcion"),
            FechaInicioActividades = ObtenerString(datos, "FechaInicioActividades"),
            EstadoContribuyente = ObtenerString(datos, "EstadoContribuyente"),
            CondicionDomicilio = ObtenerString(datos, "CondicionDomicilio"),
            DomicilioFiscal = ObtenerString(datos, "DomicilioFiscal"),
            ActividadEconomica = ObtenerString(datos, "ActividadEconomica"),
            SistemaContabilidad = ObtenerString(datos, "SistemaContabilidad"),
            ComprobantesAutorizados = ObtenerString(datos, "ComprobantesAutorizados"),
            Confianza = confianza
        };

        if (confianza.Count > 0)
            dto.ConfianzaPromedio = confianza.Values.Average();

        _logger.LogInformation(
            "Ficha RUC procesada: RUC {Ruc}, Empresa: {Empresa}, Estado: {Estado}, Confianza: {Confianza:P1}",
            dto.Ruc, dto.RazonSocial, dto.EstadoContribuyente, dto.ConfianzaPromedio);

        return dto;
    }

    // Utilidades de extraccion de JsonElement

    private static string? ObtenerString(JsonElement element, string propiedad)
    {
        if (element.TryGetProperty(propiedad, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            var val = prop.GetString();
            return string.IsNullOrWhiteSpace(val) ? null : val;
        }
        return null;
    }

    private static decimal? ObtenerDecimal(JsonElement element, string propiedad)
    {
        if (!element.TryGetProperty(propiedad, out var prop))
            return null;

        if (prop.ValueKind == JsonValueKind.Number)
            return prop.GetDecimal();

        if (prop.ValueKind == JsonValueKind.String)
        {
            var valorStr = prop.GetString()?
                .Replace(",", "")
                .Replace("S/", "")
                .Replace("$", "")
                .Replace("(", "-")
                .Replace(")", "")
                .Trim();

            if (decimal.TryParse(valorStr, out var valor))
                return valor;
        }

        return null;
    }

    private static Dictionary<string, float> ObtenerConfianzaCampos(JsonElement respuesta)
    {
        var confianza = new Dictionary<string, float>();
        if (respuesta.TryGetProperty("confianza_campos", out var campos) && campos.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in campos.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.Number)
                    confianza[prop.Name] = (float)prop.Value.GetDouble();
            }
        }
        return confianza;
    }

    private static string? LimpiarRuc(string? ruc)
    {
        if (string.IsNullOrEmpty(ruc))
            return ruc;

        return Regex.Replace(ruc, @"[^\d]", "");
    }

    private static string? LimpiarNumeroDocumentoPeru(string? numero)
    {
        if (string.IsNullOrEmpty(numero))
            return numero;

        var soloDigitos = Regex.Replace(numero, @"[^\d]", "");
        if (soloDigitos.Length >= 8)
            return soloDigitos[..8];

        return soloDigitos;
    }

    private static void CalcularRatiosFinancieros(BalanceGeneralDto balance)
    {
        if (balance.TotalActivoCorriente > 0 && balance.TotalPasivoCorriente > 0)
        {
            balance.RatioLiquidez = Math.Round(
                balance.TotalActivoCorriente.Value / balance.TotalPasivoCorriente.Value, 2);
        }

        if (balance.TotalPasivo > 0 && balance.TotalActivo > 0)
        {
            balance.RatioEndeudamiento = Math.Round(
                balance.TotalPasivo.Value / balance.TotalActivo.Value, 2);
        }

        if (balance.TotalPatrimonio > 0 && balance.TotalActivo > 0)
        {
            balance.RatioSolvencia = Math.Round(
                balance.TotalPatrimonio.Value / balance.TotalActivo.Value, 2);
        }

        if (balance.TotalActivoCorriente > 0 && balance.TotalPasivoCorriente > 0)
        {
            balance.CapitalTrabajo = Math.Round(
                balance.TotalActivoCorriente.Value - balance.TotalPasivoCorriente.Value, 2);
        }
    }
}
