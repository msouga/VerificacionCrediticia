using Microsoft.Extensions.Logging;
using VerificacionCrediticia.Core.DTOs;
using VerificacionCrediticia.Core.Entities;
using VerificacionCrediticia.Core.Enums;
using VerificacionCrediticia.Core.Interfaces;

namespace VerificacionCrediticia.Core.Services;

public class MotorReglasService : IMotorReglasService
{
    private readonly IReglaEvaluacionRepository _reglaRepository;
    private readonly ILogger<MotorReglasService> _logger;

    public MotorReglasService(
        IReglaEvaluacionRepository reglaRepository,
        ILogger<MotorReglasService> logger)
    {
        _reglaRepository = reglaRepository;
        _logger = logger;
    }

    public async Task<ResultadoMotorReglas> EvaluarAsync(Dictionary<string, object> datos)
    {
        var reglasActivas = await _reglaRepository.GetActivasAsync();
        return await EvaluarContraReglas(datos, reglasActivas);
    }

    public async Task<ResultadoMotorReglas> EvaluarAsync(Dictionary<string, object> datos, List<int> reglasIds)
    {
        var todasLasReglas = await _reglaRepository.GetActivasAsync();
        var reglasFiltradas = todasLasReglas.Where(r => reglasIds.Contains(r.Id)).ToList();
        return await EvaluarContraReglas(datos, reglasFiltradas);
    }

    private Task<ResultadoMotorReglas> EvaluarContraReglas(
        Dictionary<string, object> datos,
        List<ReglaEvaluacion> reglas)
    {
        _logger.LogInformation("Iniciando evaluación de {CantidadReglas} reglas contra {CantidadDatos} datos",
            reglas.Count, datos.Count);

        var resultado = new ResultadoMotorReglas();
        var reglasAplicadas = new List<ResultadoReglaAplicada>();
        var rechazosInmediatos = new List<ResultadoReglaAplicada>();

        // Evaluar cada regla
        foreach (var regla in reglas)
        {
            var resultadoRegla = EvaluarReglaIndividual(regla, datos);
            reglasAplicadas.Add(resultadoRegla);

            // Si es un rechazo inmediato, guardarlo
            if (!resultadoRegla.Cumplida && regla.Resultado == ResultadoRegla.Rechazar)
            {
                rechazosInmediatos.Add(resultadoRegla);
            }
        }

        // Determinar recomendación final
        DeterminarRecomendacion(resultado, reglasAplicadas, rechazosInmediatos);

        // Calcular puntaje ponderado
        CalcularPuntaje(resultado, reglasAplicadas);

        // Determinar nivel de riesgo
        DeterminarNivelRiesgo(resultado);

        // Generar resumen
        GenerarResumen(resultado, reglasAplicadas, rechazosInmediatos);

        resultado.ReglasAplicadas = reglasAplicadas;

        _logger.LogInformation(
            "Evaluación completada: {Recomendacion}, Puntaje: {Puntaje}, Riesgo: {NivelRiesgo}",
            resultado.Recomendacion, resultado.PuntajeFinal, resultado.NivelRiesgo);

        return Task.FromResult(resultado);
    }

    private ResultadoReglaAplicada EvaluarReglaIndividual(ReglaEvaluacion regla, Dictionary<string, object> datos)
    {
        var resultado = new ResultadoReglaAplicada
        {
            ReglaId = regla.Id,
            NombreRegla = regla.Nombre,
            CampoEvaluado = regla.Campo,
            OperadorUtilizado = ObtenerDescripcionOperador(regla.Operador),
            ValorEsperado = regla.Valor,
            Peso = regla.Peso,
            ResultadoRegla = regla.Resultado,
            Descripcion = regla.Descripcion ?? string.Empty
        };

        // Buscar el valor en los datos
        if (!datos.TryGetValue(regla.Campo, out var valorObj))
        {
            _logger.LogWarning("Campo {Campo} no encontrado en los datos para regla {ReglaId}",
                regla.Campo, regla.Id);

            resultado.Cumplida = false;
            resultado.Mensaje = $"Campo {regla.Campo} no encontrado en los datos";
            return resultado;
        }

        // Convertir valor a decimal para comparación
        if (!TryConvertToDecimal(valorObj, out var valorDecimal))
        {
            _logger.LogWarning("No se pudo convertir el valor {Valor} del campo {Campo} a decimal para regla {ReglaId}",
                valorObj, regla.Campo, regla.Id);

            resultado.Cumplida = false;
            resultado.ValorReal = null;
            resultado.Mensaje = $"Valor no numérico: {valorObj}";
            return resultado;
        }

        resultado.ValorReal = valorDecimal;

        // Evaluar la condición según el operador
        bool condicionCumplida = regla.Operador switch
        {
            OperadorComparacion.MayorQue => valorDecimal > regla.Valor,
            OperadorComparacion.MenorQue => valorDecimal < regla.Valor,
            OperadorComparacion.MayorOIgualQue => valorDecimal >= regla.Valor,
            OperadorComparacion.MenorOIgualQue => valorDecimal <= regla.Valor,
            OperadorComparacion.IgualQue => Math.Abs(valorDecimal - regla.Valor) < 0.0001m,
            OperadorComparacion.DiferenteDe => Math.Abs(valorDecimal - regla.Valor) >= 0.0001m,
            _ => false
        };

        resultado.Cumplida = condicionCumplida;
        resultado.Mensaje = GenerarMensajeResultado(resultado, condicionCumplida);

        _logger.LogDebug("Regla {ReglaId} evaluada: {Campo} {Operador} {ValorEsperado} | Valor real: {ValorReal} | Resultado: {Cumplida}",
            regla.Id, regla.Campo, resultado.OperadorUtilizado, regla.Valor, valorDecimal, condicionCumplida);

        return resultado;
    }

    private static bool TryConvertToDecimal(object valor, out decimal resultado)
    {
        resultado = 0m;

        switch (valor)
        {
            case decimal d:
                resultado = d;
                return true;
            case double db:
                resultado = (decimal)db;
                return true;
            case float f:
                resultado = (decimal)f;
                return true;
            case int i:
                resultado = i;
                return true;
            case long l:
                resultado = l;
                return true;
            case string s when decimal.TryParse(s, out var parsed):
                resultado = parsed;
                return true;
            default:
                return false;
        }
    }

    private static string ObtenerDescripcionOperador(OperadorComparacion operador)
    {
        return operador switch
        {
            OperadorComparacion.MayorQue => ">",
            OperadorComparacion.MenorQue => "<",
            OperadorComparacion.MayorOIgualQue => ">=",
            OperadorComparacion.MenorOIgualQue => "<=",
            OperadorComparacion.IgualQue => "==",
            OperadorComparacion.DiferenteDe => "!=",
            _ => "??"
        };
    }

    private static string GenerarMensajeResultado(ResultadoReglaAplicada regla, bool cumplida)
    {
        var simboloResultado = cumplida ? "✓" : "✗";
        var valorReal = regla.ValorReal?.ToString("N2") ?? "N/A";

        return $"{simboloResultado} {regla.CampoEvaluado}: {valorReal} {regla.OperadorUtilizado} {regla.ValorEsperado:N2}";
    }

    private static void DeterminarRecomendacion(
        ResultadoMotorReglas resultado,
        List<ResultadoReglaAplicada> reglasAplicadas,
        List<ResultadoReglaAplicada> rechazosInmediatos)
    {
        // Si hay rechazos inmediatos, rechazar
        if (rechazosInmediatos.Count > 0)
        {
            resultado.Recomendacion = Recomendacion.Rechazar;
            return;
        }

        // Calcular porcentaje de cumplimiento ponderado
        var reglasPorResultado = reglasAplicadas
            .Where(r => r.Peso > 0)
            .GroupBy(r => r.ResultadoRegla)
            .ToList();

        var pesoTotalAprobar = reglasPorResultado
            .Where(g => g.Key == ResultadoRegla.Aprobar)
            .SelectMany(g => g)
            .Where(r => r.Cumplida)
            .Sum(r => r.Peso);

        var pesoTotalRevisar = reglasPorResultado
            .Where(g => g.Key == ResultadoRegla.Revisar)
            .SelectMany(g => g)
            .Where(r => !r.Cumplida)
            .Sum(r => r.Peso);

        var pesoTotal = reglasAplicadas.Where(r => r.Peso > 0).Sum(r => r.Peso);

        if (pesoTotal == 0)
        {
            resultado.Recomendacion = Recomendacion.RevisarManualmente;
            return;
        }

        var porcentajeCumplimiento = (pesoTotalAprobar / pesoTotal) * 100;

        // Determinar recomendación basada en porcentaje
        resultado.Recomendacion = porcentajeCumplimiento switch
        {
            >= 80m => Recomendacion.Aprobar,
            >= 50m when pesoTotalRevisar > 0 => Recomendacion.RevisarManualmente,
            >= 50m => Recomendacion.Aprobar,
            _ => Recomendacion.Rechazar
        };
    }

    private static void CalcularPuntaje(ResultadoMotorReglas resultado, List<ResultadoReglaAplicada> reglasAplicadas)
    {
        var pesoTotal = reglasAplicadas.Where(r => r.Peso > 0).Sum(r => r.Peso);

        if (pesoTotal == 0)
        {
            resultado.PuntajeFinal = 0;
            return;
        }

        var puntajeAcumulado = 0m;

        foreach (var regla in reglasAplicadas.Where(r => r.Peso > 0))
        {
            var puntosPorRegla = regla.ResultadoRegla switch
            {
                ResultadoRegla.Aprobar when regla.Cumplida => regla.Peso,
                ResultadoRegla.Revisar when regla.Cumplida => regla.Peso * 0.7m, // Penalización por revisar
                ResultadoRegla.Rechazar when !regla.Cumplida => 0m, // No suma puntos si falla
                _ => regla.Peso * 0.3m // Puntos parciales
            };

            puntajeAcumulado += puntosPorRegla;
        }

        resultado.PuntajeFinal = Math.Round((puntajeAcumulado / pesoTotal) * 100, 2);
    }

    private static void DeterminarNivelRiesgo(ResultadoMotorReglas resultado)
    {
        resultado.NivelRiesgo = resultado.PuntajeFinal switch
        {
            >= 80m => NivelRiesgo.Bajo,
            >= 60m => NivelRiesgo.Moderado,
            >= 40m => NivelRiesgo.Alto,
            _ => NivelRiesgo.MuyAlto
        };
    }

    private static void GenerarResumen(
        ResultadoMotorReglas resultado,
        List<ResultadoReglaAplicada> reglasAplicadas,
        List<ResultadoReglaAplicada> rechazosInmediatos)
    {
        var cumplidas = reglasAplicadas.Count(r => r.Cumplida);
        var total = reglasAplicadas.Count;

        var resumen = $"Evaluación: {cumplidas}/{total} reglas cumplidas ({resultado.PuntajeFinal:F1}%)";

        if (rechazosInmediatos.Count > 0)
        {
            var motivosRechazo = string.Join(", ", rechazosInmediatos.Select(r => r.CampoEvaluado));
            resumen += $". Rechazado por: {motivosRechazo}";
        }

        resultado.Resumen = resumen;
    }
}