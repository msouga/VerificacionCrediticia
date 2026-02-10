using VerificacionCrediticia.Core.DTOs;

namespace VerificacionCrediticia.Core.Interfaces;

/// <summary>
/// Servicio para evaluar datos contra reglas parametrizables de negocio
/// </summary>
public interface IMotorReglasService
{
    /// <summary>
    /// Evalúa un conjunto de datos contra todas las reglas activas
    /// </summary>
    /// <param name="datos">Diccionario con los datos a evaluar (campo -> valor)</param>
    /// <returns>Resultado de la evaluación con recomendación y detalles</returns>
    Task<ResultadoMotorReglas> EvaluarAsync(Dictionary<string, object> datos);

    /// <summary>
    /// Evalúa un conjunto de datos contra reglas específicas
    /// </summary>
    /// <param name="datos">Diccionario con los datos a evaluar</param>
    /// <param name="reglasIds">IDs de las reglas específicas a evaluar</param>
    /// <returns>Resultado de la evaluación</returns>
    Task<ResultadoMotorReglas> EvaluarAsync(Dictionary<string, object> datos, List<int> reglasIds);
}