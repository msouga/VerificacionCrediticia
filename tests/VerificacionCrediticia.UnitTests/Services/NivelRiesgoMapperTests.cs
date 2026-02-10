using VerificacionCrediticia.Core.Enums;
using VerificacionCrediticia.Core.Services;
using Xunit;

namespace VerificacionCrediticia.UnitTests.Services;

public class NivelRiesgoMapperTests
{
    [Theory]
    // Formato API real Equifax LATAM Peru
    [InlineData("MUY BAJO", NivelRiesgo.MuyBajo)]
    [InlineData("BAJO", NivelRiesgo.Bajo)]
    [InlineData("MEDIO", NivelRiesgo.Moderado)]
    [InlineData("ALTO", NivelRiesgo.Alto)]
    [InlineData("MUY ALTO", NivelRiesgo.MuyAlto)]
    // Formato mock (compatibilidad)
    [InlineData("RIESGO BAJO", NivelRiesgo.Bajo)]
    [InlineData("RIESGO MODERADO", NivelRiesgo.Moderado)]
    [InlineData("RIESGO ALTO", NivelRiesgo.Alto)]
    [InlineData("RIESGO MUY ALTO", NivelRiesgo.MuyAlto)]
    // Case insensitive
    [InlineData("riesgo bajo", NivelRiesgo.Bajo)]
    [InlineData("Riesgo Alto", NivelRiesgo.Alto)]
    [InlineData("medio", NivelRiesgo.Moderado)]
    [InlineData("muy bajo", NivelRiesgo.MuyBajo)]
    public void ParseRiesgo_TextoValido_RetornaNivelCorrecto(string texto, NivelRiesgo esperado)
    {
        var resultado = NivelRiesgoMapper.ParseRiesgo(texto);
        Assert.Equal(esperado, resultado);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("DESCONOCIDO")]
    [InlineData("OTRO VALOR")]
    public void ParseRiesgo_TextoInvalido_RetornaModerado(string? texto)
    {
        var resultado = NivelRiesgoMapper.ParseRiesgo(texto);
        Assert.Equal(NivelRiesgo.Moderado, resultado);
    }

    [Theory]
    [InlineData(NivelRiesgo.MuyBajo, EstadoCrediticio.Normal)]
    [InlineData(NivelRiesgo.Bajo, EstadoCrediticio.Normal)]
    [InlineData(NivelRiesgo.Moderado, EstadoCrediticio.ConProblemasPotenciales)]
    [InlineData(NivelRiesgo.Alto, EstadoCrediticio.Moroso)]
    [InlineData(NivelRiesgo.MuyAlto, EstadoCrediticio.Castigado)]
    public void ToEstadoCrediticio_RetornaEstadoCorrecto(NivelRiesgo riesgo, EstadoCrediticio esperado)
    {
        var resultado = NivelRiesgoMapper.ToEstadoCrediticio(riesgo);
        Assert.Equal(esperado, resultado);
    }

    [Theory]
    [InlineData(NivelRiesgo.MuyBajo, 900)]
    [InlineData(NivelRiesgo.Bajo, 800)]
    [InlineData(NivelRiesgo.Moderado, 550)]
    [InlineData(NivelRiesgo.Alto, 350)]
    [InlineData(NivelRiesgo.MuyAlto, 150)]
    public void ToScoreNumerico_RetornaScoreCorrecto(NivelRiesgo riesgo, decimal esperado)
    {
        var resultado = NivelRiesgoMapper.ToScoreNumerico(riesgo);
        Assert.Equal(esperado, resultado);
    }

    [Theory]
    [InlineData(NivelRiesgo.MuyBajo, "MUY BAJO")]
    [InlineData(NivelRiesgo.Bajo, "BAJO")]
    [InlineData(NivelRiesgo.Moderado, "MEDIO")]
    [InlineData(NivelRiesgo.Alto, "ALTO")]
    [InlineData(NivelRiesgo.MuyAlto, "MUY ALTO")]
    public void ToTextoEquifax_RetornaTextoCorrecto(NivelRiesgo riesgo, string esperado)
    {
        var resultado = NivelRiesgoMapper.ToTextoEquifax(riesgo);
        Assert.Equal(esperado, resultado);
    }

    [Fact]
    public void RoundTrip_ParseYToTexto_SonConsistentes()
    {
        var niveles = new[] { NivelRiesgo.MuyBajo, NivelRiesgo.Bajo, NivelRiesgo.Moderado, NivelRiesgo.Alto, NivelRiesgo.MuyAlto };

        foreach (var nivel in niveles)
        {
            var texto = NivelRiesgoMapper.ToTextoEquifax(nivel);
            var parseado = NivelRiesgoMapper.ParseRiesgo(texto);
            Assert.Equal(nivel, parseado);
        }
    }

    [Fact]
    public void ToScoreNumerico_MuyBajo_SuperaUmbralAprobacion()
    {
        var score = NivelRiesgoMapper.ToScoreNumerico(NivelRiesgo.MuyBajo);
        Assert.True(score >= 600, "Riesgo Muy Bajo debe superar umbral de aprobacion (600)");
    }

    [Fact]
    public void ToScoreNumerico_Bajo_SuperaUmbralAprobacion()
    {
        var score = NivelRiesgoMapper.ToScoreNumerico(NivelRiesgo.Bajo);
        Assert.True(score >= 600, "Riesgo Bajo debe superar umbral de aprobacion (600)");
    }

    [Fact]
    public void ToScoreNumerico_MuyAlto_NoCumpleUmbral()
    {
        var score = NivelRiesgoMapper.ToScoreNumerico(NivelRiesgo.MuyAlto);
        Assert.True(score < 600, "Riesgo Muy Alto no debe cumplir umbral de aprobacion");
    }
}
