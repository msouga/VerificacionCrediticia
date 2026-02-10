using VerificacionCrediticia.Core.Enums;

namespace VerificacionCrediticia.Core.Services;

public static class NivelRiesgoMapper
{
    public static NivelRiesgo ParseRiesgo(string? riesgoTexto)
    {
        return riesgoTexto?.ToUpperInvariant() switch
        {
            // Formato API real Equifax LATAM Peru
            "MUY BAJO" => NivelRiesgo.MuyBajo,
            "BAJO" => NivelRiesgo.Bajo,
            "MEDIO" => NivelRiesgo.Moderado,
            "ALTO" => NivelRiesgo.Alto,
            "MUY ALTO" => NivelRiesgo.MuyAlto,
            // Formato mock (compatibilidad)
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
            NivelRiesgo.MuyBajo => EstadoCrediticio.Normal,
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
            NivelRiesgo.MuyBajo => 900m,
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
            NivelRiesgo.MuyBajo => "MUY BAJO",
            NivelRiesgo.Bajo => "BAJO",
            NivelRiesgo.Moderado => "MEDIO",
            NivelRiesgo.Alto => "ALTO",
            NivelRiesgo.MuyAlto => "MUY ALTO",
            _ => "MEDIO"
        };
    }
}
