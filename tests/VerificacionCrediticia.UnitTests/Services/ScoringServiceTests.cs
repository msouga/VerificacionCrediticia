using VerificacionCrediticia.Core.DTOs;
using VerificacionCrediticia.Core.Entities;
using VerificacionCrediticia.Core.Enums;
using VerificacionCrediticia.Core.Services;
using Xunit;

namespace VerificacionCrediticia.UnitTests.Services;

public class ScoringServiceTests
{
    private readonly ScoringService _scoringService;

    public ScoringServiceTests()
    {
        _scoringService = new ScoringService();
    }

    [Fact]
    public void EvaluarRed_SinProblemas_DebeAprobar()
    {
        // Arrange
        var exploracion = new ResultadoExploracionDto
        {
            DniSolicitante = "12345678",
            RucEmpresa = "20123456789",
            Grafo = new Dictionary<string, NodoRed>
            {
                ["12345678"] = new NodoRed
                {
                    Identificador = "12345678",
                    Tipo = TipoNodo.Persona,
                    Nombre = "Juan Pérez",
                    NivelProfundidad = 0,
                    Score = 750,
                    EstadoCredito = EstadoCrediticio.Normal,
                    Alertas = new List<string>(),
                    Deudas = new List<DeudaRegistrada>()
                },
                ["20123456789"] = new NodoRed
                {
                    Identificador = "20123456789",
                    Tipo = TipoNodo.Empresa,
                    Nombre = "Empresa SAC",
                    NivelProfundidad = 0,
                    Score = 700,
                    EstadoCredito = EstadoCrediticio.Normal,
                    Alertas = new List<string>(),
                    Deudas = new List<DeudaRegistrada>()
                }
            },
            TotalNodos = 2,
            TotalPersonas = 1,
            TotalEmpresas = 1
        };

        // Act
        var resultado = _scoringService.EvaluarRed(exploracion);

        // Assert
        Assert.Equal(Recomendacion.Aprobar, resultado.Recomendacion);
        Assert.True(resultado.ScoreFinal >= 60);
        Assert.Empty(resultado.Alertas);
    }

    [Fact]
    public void EvaluarRed_ConMorosidadNivel0_DebeRechazar()
    {
        // Arrange
        var exploracion = new ResultadoExploracionDto
        {
            DniSolicitante = "12345678",
            RucEmpresa = "20123456789",
            Grafo = new Dictionary<string, NodoRed>
            {
                ["12345678"] = new NodoRed
                {
                    Identificador = "12345678",
                    Tipo = TipoNodo.Persona,
                    Nombre = "Juan Pérez",
                    NivelProfundidad = 0,
                    Score = 300,
                    EstadoCredito = EstadoCrediticio.Moroso,
                    Alertas = new List<string> { "Persona en morosidad" },
                    Deudas = new List<DeudaRegistrada>()
                },
                ["20123456789"] = new NodoRed
                {
                    Identificador = "20123456789",
                    Tipo = TipoNodo.Empresa,
                    Nombre = "Empresa SAC",
                    NivelProfundidad = 0,
                    Score = 400,
                    EstadoCredito = EstadoCrediticio.Normal,
                    Alertas = new List<string>(),
                    Deudas = new List<DeudaRegistrada>()
                }
            },
            TotalNodos = 2,
            TotalPersonas = 1,
            TotalEmpresas = 1
        };

        // Act
        var resultado = _scoringService.EvaluarRed(exploracion);

        // Assert
        Assert.Equal(Recomendacion.Rechazar, resultado.Recomendacion);
        Assert.True(resultado.Alertas.Any(a => a.Tipo == TipoAlerta.Morosidad));
    }

    [Fact]
    public void EvaluarRed_ConProblemasEnNivel2_DebeRevisarManualmente()
    {
        // Arrange
        var exploracion = new ResultadoExploracionDto
        {
            DniSolicitante = "12345678",
            RucEmpresa = "20123456789",
            Grafo = new Dictionary<string, NodoRed>
            {
                ["12345678"] = new NodoRed
                {
                    Identificador = "12345678",
                    Tipo = TipoNodo.Persona,
                    Nombre = "Juan Pérez",
                    NivelProfundidad = 0,
                    Score = 650,
                    EstadoCredito = EstadoCrediticio.Normal,
                    Alertas = new List<string>(),
                    Deudas = new List<DeudaRegistrada>()
                },
                ["20123456789"] = new NodoRed
                {
                    Identificador = "20123456789",
                    Tipo = TipoNodo.Empresa,
                    Nombre = "Empresa SAC",
                    NivelProfundidad = 0,
                    Score = 600,
                    EstadoCredito = EstadoCrediticio.Normal,
                    Alertas = new List<string>(),
                    Deudas = new List<DeudaRegistrada>()
                },
                ["87654321"] = new NodoRed
                {
                    Identificador = "87654321",
                    Tipo = TipoNodo.Persona,
                    Nombre = "Pedro García",
                    NivelProfundidad = 2,
                    Score = 400,
                    EstadoCredito = EstadoCrediticio.ConProblemasPotenciales,
                    Alertas = new List<string> { "Score bajo" },
                    Deudas = new List<DeudaRegistrada>()
                }
            },
            TotalNodos = 3,
            TotalPersonas = 2,
            TotalEmpresas = 1
        };

        // Act
        var resultado = _scoringService.EvaluarRed(exploracion);

        // Assert
        Assert.True(resultado.ScoreFinal > 40);
        Assert.True(resultado.Alertas.Any());
    }
}
