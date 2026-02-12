using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VerificacionCrediticia.Core.DTOs;
using VerificacionCrediticia.Core.Entities;
using VerificacionCrediticia.Core.Enums;
using VerificacionCrediticia.Core.Interfaces;

namespace VerificacionCrediticia.Infrastructure.AzureOpenAI;

public class NarrativaEvaluacionService : INarrativaEvaluacionService
{
    private readonly HttpClient _httpClient;
    private readonly AzureOpenAISettings _settings;
    private readonly ILogger<NarrativaEvaluacionService> _logger;

    private const string SystemPrompt = """
        Eres un analista de credito senior de una empresa financiera en Peru.
        Tu tarea es redactar un informe narrativo profesional sobre la evaluacion crediticia de un solicitante.

        Instrucciones:
        - Escribe en tercera persona, en espanol formal pero claro
        - Estructura el informe en parrafos fluidos (NO uses vinetas ni listas)
        - Comienza identificando al solicitante y la empresa
        - Describe los hallazgos positivos y negativos de manera equilibrada
        - Menciona los ratios financieros relevantes y su interpretacion
        - Si hubo problemas en las validaciones cruzadas, explicalos
        - Concluye con la recomendacion final y, si aplica, el monto de linea de credito sugerido
        - Manten un tono objetivo y profesional
        - Maximo 4 parrafos
        - NO uses markdown, vinetas, asteriscos ni formato especial. Solo texto plano con saltos de linea entre parrafos.
        """;

    public NarrativaEvaluacionService(
        HttpClient httpClient,
        IOptions<AzureOpenAISettings> settings,
        ILogger<NarrativaEvaluacionService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string> GenerarNarrativaAsync(
        Expediente expediente,
        ResultadoMotorReglas resultado,
        Dictionary<string, object> datosEvaluacion,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var contexto = ConstruirContextoEvaluacion(expediente, resultado, datosEvaluacion);
            var narrativa = await LlamarGpt41Async(contexto, cancellationToken);
            _logger.LogInformation("Narrativa generada para expediente {Id} ({Length} caracteres)",
                expediente.Id, narrativa.Length);
            return narrativa;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error generando narrativa con GPT-4.1 para expediente {Id}, usando fallback",
                expediente.Id);
            return GenerarResumenBasico(expediente, resultado);
        }
    }

    private string ConstruirContextoEvaluacion(
        Expediente expediente,
        ResultadoMotorReglas resultado,
        Dictionary<string, object> datosEvaluacion)
    {
        var sb = new StringBuilder();
        sb.AppendLine("DATOS DE LA EVALUACION CREDITICIA:");
        sb.AppendLine();

        // Solicitante y empresa
        sb.AppendLine($"Solicitante: {expediente.NombresSolicitante} {expediente.ApellidosSolicitante} (DNI: {expediente.DniSolicitante})");
        sb.AppendLine($"Empresa: {expediente.RazonSocialEmpresa} (RUC: {expediente.RucEmpresa})");
        sb.AppendLine();

        // Score y recomendacion
        sb.AppendLine($"Score final: {resultado.PuntajeFinal:F1} / 100");
        sb.AppendLine($"Recomendacion: {resultado.Recomendacion}");
        sb.AppendLine($"Nivel de riesgo: {resultado.NivelRiesgo}");
        sb.AppendLine();

        // Ratios financieros
        sb.AppendLine("RATIOS FINANCIEROS:");
        foreach (var dato in datosEvaluacion)
        {
            sb.AppendLine($"  {dato.Key}: {dato.Value}");
        }
        sb.AppendLine();

        // Reglas aplicadas
        if (resultado.ReglasAplicadas.Count > 0)
        {
            sb.AppendLine("REGLAS APLICADAS:");
            foreach (var regla in resultado.ReglasAplicadas)
            {
                var estado = regla.Cumplida ? "CUMPLIDA" : "NO CUMPLIDA";
                sb.AppendLine($"  - {regla.NombreRegla}: {regla.CampoEvaluado} {regla.OperadorUtilizado} {regla.ValorEsperado} " +
                    $"(valor real: {regla.ValorReal}) -> {estado} [{regla.ResultadoRegla}]");
            }
            sb.AppendLine();
        }

        // Validaciones cruzadas
        if (resultado.ValidacionesCruzadas.Count > 0)
        {
            sb.AppendLine("VALIDACIONES CRUZADAS DE DOCUMENTOS:");
            foreach (var v in resultado.ValidacionesCruzadas)
            {
                var estado = v.Aprobada ? "APROBADA" : "FALLIDA";
                sb.AppendLine($"  - {v.Nombre}: {estado} - {v.Mensaje}");
            }
            sb.AppendLine();
        }

        // Red de relaciones (Equifax)
        if (resultado.ExploracionRed != null)
        {
            var red = resultado.ExploracionRed;
            sb.AppendLine($"RED DE RELACIONES CREDITICIAS ({red.TotalNodos} nodos: {red.TotalPersonas} personas, {red.TotalEmpresas} empresas):");
            foreach (var (id, nodo) in red.Grafo)
            {
                var tipo = nodo.Tipo == TipoNodo.Persona ? "Persona" : "Empresa";
                sb.AppendLine($"  [{tipo}] {nodo.Nombre} ({id}) - Nivel {nodo.NivelProfundidad} - {nodo.NivelRiesgoTexto ?? "Sin info"}");
                if (nodo.Alertas.Count > 0)
                    sb.AppendLine($"    Alertas: {string.Join("; ", nodo.Alertas)}");
                if (nodo.Deudas.Count > 0)
                {
                    var deudasVencidas = nodo.Deudas.Where(d => d.EstaVencida).ToList();
                    if (deudasVencidas.Count > 0)
                        sb.AppendLine($"    Deudas vencidas: {deudasVencidas.Count} (total S/ {deudasVencidas.Sum(d => d.SaldoActual):N2})");
                }
            }
            sb.AppendLine();
        }

        // Linea de credito
        if (resultado.LineaCredito != null)
        {
            var lc = resultado.LineaCredito;
            sb.AppendLine($"LINEA DE CREDITO SUGERIDA: {lc.Moneda} {lc.MontoMaximoSugerido:N2}");
            sb.AppendLine($"Justificacion: {lc.Justificacion}");
            foreach (var d in lc.Detalles)
            {
                sb.AppendLine($"  - {d.Concepto}: {d.Porcentaje}% de {d.ValorBase:N2} = {d.MontoCalculado:N2}");
            }
        }

        return sb.ToString();
    }

    private async Task<string> LlamarGpt41Async(string contexto, CancellationToken cancellationToken)
    {
        var requestBody = new
        {
            messages = new object[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user", content = $"Genera el informe narrativo para la siguiente evaluacion crediticia:\n\n{contexto}" }
            },
            max_tokens = 1500,
            temperature = 0.3
        };

        var endpoint = _settings.Endpoint.TrimEnd('/');
        var url = $"{endpoint}/openai/deployments/{_settings.DeploymentName}/chat/completions?api-version={_settings.ApiVersion}";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("api-key", _settings.ApiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Error HTTP {Status} de Azure OpenAI para narrativa: {Body}",
                (int)response.StatusCode, errorBody.Length > 500 ? errorBody[..500] : errorBody);
            throw new HttpRequestException($"Azure OpenAI respondio {(int)response.StatusCode}", null, response.StatusCode);
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseJson);

        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return content?.Trim() ?? GenerarResumenBasico(null, null);
    }

    internal static string GenerarResumenBasico(Expediente? expediente, ResultadoMotorReglas? resultado)
    {
        if (expediente == null || resultado == null)
            return "No se pudo generar el informe narrativo.";

        var sb = new StringBuilder();

        var nombre = $"{expediente.NombresSolicitante} {expediente.ApellidosSolicitante}".Trim();
        if (!string.IsNullOrEmpty(nombre))
            sb.Append($"Evaluacion crediticia de {nombre}");
        else
            sb.Append("Evaluacion crediticia del solicitante");

        if (!string.IsNullOrEmpty(expediente.RazonSocialEmpresa))
            sb.Append($" para la empresa {expediente.RazonSocialEmpresa}");

        sb.Append($". Score obtenido: {resultado.PuntajeFinal:F1}/100.");
        sb.Append($" Nivel de riesgo: {resultado.NivelRiesgo}.");
        sb.Append($" Recomendacion: {resultado.Recomendacion}.");

        if (resultado.LineaCredito != null)
        {
            sb.Append($" Linea de credito sugerida: {resultado.LineaCredito.Moneda} {resultado.LineaCredito.MontoMaximoSugerido:N2}.");
        }

        return sb.ToString();
    }
}

public class NarrativaEvaluacionServiceMock : INarrativaEvaluacionService
{
    public Task<string> GenerarNarrativaAsync(
        Expediente expediente,
        ResultadoMotorReglas resultado,
        Dictionary<string, object> datosEvaluacion,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(NarrativaEvaluacionService.GenerarResumenBasico(expediente, resultado));
    }
}
