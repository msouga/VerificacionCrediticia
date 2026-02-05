using VerificacionCrediticia.Core.Enums;

namespace VerificacionCrediticia.Core.Services;

public static class NivelRiesgoMapper
{
    public static NivelRiesgo ParseRiesgo(string? riesgoTexto)
    {
        return riesgoTexto?.ToUpperInvariant() switch
        {
            "RIESGO BAJO" => NivelRiesgo.Bajo,
            "RIESGO MODERADO" => NivelRiesgo.Moderado,
            "RIESGO ALTO" => NivelRiesgo.Alto,
            "RIESGO MUY ALTO" => NivelRiesgo.MuyAlto,
            _ => NivelRiesgo.Moderado
        };
    }

    public static EstadoCrediticio ToEstadoCrediticio(NivelRiesgo riesgo)
    {
        return riesgo switch
        {
            NivelRiesgo.Bajo => EstadoCrediticio.Normal,
            NivelRiesgo.Moderado => EstadoCrediticio.ConProblemasPotenciales,
            NivelRiesgo.Alto => EstadoCrediticio.Moroso,
            NivelRiesgo.MuyAlto => EstadoCrediticio.Castigado,
            _ => EstadoCrediticio.SinInformacion
        };
    }

    public static decimal ToScoreNumerico(NivelRiesgo riesgo)
    {
        return riesgo switch
        {
            NivelRiesgo.Bajo => 800m,
            NivelRiesgo.Moderado => 550m,
            NivelRiesgo.Alto => 350m,
            NivelRiesgo.MuyAlto => 150m,
            _ => 400m
        };
    }

    public static string ToTextoEquifax(NivelRiesgo riesgo)
    {
        return riesgo switch
        {
            NivelRiesgo.Bajo => "RIESGO BAJO",
            NivelRiesgo.Moderado => "RIESGO MODERADO",
            NivelRiesgo.Alto => "RIESGO ALTO",
            NivelRiesgo.MuyAlto => "RIESGO MUY ALTO",
            _ => "RIESGO MODERADO"
        };
    }
}
