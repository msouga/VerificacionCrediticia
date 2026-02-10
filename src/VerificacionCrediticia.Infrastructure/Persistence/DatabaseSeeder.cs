using Microsoft.Extensions.Logging;
using VerificacionCrediticia.Core.Entities;
using VerificacionCrediticia.Core.Enums;

namespace VerificacionCrediticia.Infrastructure.Persistence;

public class DatabaseSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(ApplicationDbContext context, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            await _context.Database.EnsureCreatedAsync();

            await SeedTiposDocumentoAsync();
            await SeedReglasEvaluacionAsync();

            await _context.SaveChangesAsync();

            _logger.LogInformation("Database seeded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding database");
            throw;
        }
    }

    private Task SeedTiposDocumentoAsync()
    {
        if (_context.TiposDocumento.Any())
            return Task.CompletedTask;

        var tiposDocumento = new List<TipoDocumento>
        {
            new()
            {
                Nombre = "DNI",
                Codigo = "DNI",
                AnalyzerId = "dniperuano",
                EsObligatorio = true,
                Orden = 1,
                Descripcion = "Documento Nacional de Identidad del solicitante",
                Activo = true
            },
            new()
            {
                Nombre = "Vigencia de Poder",
                Codigo = "VIGENCIA_PODER",
                AnalyzerId = "vigenciaPoderes",
                EsObligatorio = true,
                Orden = 2,
                Descripcion = "Vigencia de Poder de la empresa representada",
                Activo = true
            },
            new()
            {
                Nombre = "Balance General",
                Codigo = "BALANCE_GENERAL",
                AnalyzerId = "balanceGeneral",
                EsObligatorio = true,
                Orden = 3,
                Descripcion = "Balance General de la empresa",
                Activo = true
            },
            new()
            {
                Nombre = "Estado de Resultados",
                Codigo = "ESTADO_RESULTADOS",
                AnalyzerId = "estadoResultados",
                EsObligatorio = true,
                Orden = 4,
                Descripcion = "Estado de Resultados de la empresa",
                Activo = true
            },
            new()
            {
                Nombre = "Ficha RUC",
                Codigo = "FICHA_RUC",
                AnalyzerId = "fichaRuc",
                EsObligatorio = false,
                Orden = 5,
                Descripcion = "Ficha RUC de la empresa",
                Activo = true
            }
        };

        _context.TiposDocumento.AddRange(tiposDocumento);
        _logger.LogInformation("Added {Count} tipos de documento", tiposDocumento.Count);
        return Task.CompletedTask;
    }

    private Task SeedReglasEvaluacionAsync()
    {
        if (_context.ReglasEvaluacion.Any())
            return Task.CompletedTask;

        var reglas = new List<ReglaEvaluacion>
        {
            new()
            {
                Nombre = "Liquidez Mínima Aceptable",
                Descripcion = "La empresa debe tener una liquidez mayor o igual a 1.5",
                Campo = "Liquidez",
                Operador = OperadorComparacion.MayorOIgualQue,
                Valor = 1.5m,
                Peso = 0.20m,
                Resultado = ResultadoRegla.Aprobar,
                Activa = true,
                Orden = 1
            },
            new()
            {
                Nombre = "Liquidez Crítica",
                Descripcion = "Empresas con liquidez menor a 1.0 deben ser rechazadas",
                Campo = "Liquidez",
                Operador = OperadorComparacion.MenorQue,
                Valor = 1.0m,
                Peso = 0.25m,
                Resultado = ResultadoRegla.Rechazar,
                Activa = true,
                Orden = 2
            },
            new()
            {
                Nombre = "Endeudamiento Alto",
                Descripcion = "Empresas con endeudamiento mayor a 0.7 requieren revisión manual",
                Campo = "Endeudamiento",
                Operador = OperadorComparacion.MayorQue,
                Valor = 0.7m,
                Peso = 0.15m,
                Resultado = ResultadoRegla.Revisar,
                Activa = true,
                Orden = 3
            },
            new()
            {
                Nombre = "Score Mínimo",
                Descripcion = "El score crediticio debe ser mayor o igual a 300",
                Campo = "ScoreCrediticio",
                Operador = OperadorComparacion.MenorQue,
                Valor = 300m,
                Peso = 0.30m,
                Resultado = ResultadoRegla.Rechazar,
                Activa = true,
                Orden = 4
            },
            new()
            {
                Nombre = "Sin Deuda Vencida",
                Descripcion = "No debe tener deuda vencida en el sistema financiero",
                Campo = "DeudaVencida",
                Operador = OperadorComparacion.MayorQue,
                Valor = 0m,
                Peso = 0.20m,
                Resultado = ResultadoRegla.Revisar,
                Activa = true,
                Orden = 5
            },
            new()
            {
                Nombre = "Margen Neto Aceptable",
                Descripcion = "El margen neto debe ser mayor o igual a 5%",
                Campo = "MargenNeto",
                Operador = OperadorComparacion.MayorOIgualQue,
                Valor = 0.05m,
                Peso = 0.15m,
                Resultado = ResultadoRegla.Aprobar,
                Activa = true,
                Orden = 6
            },
            new()
            {
                Nombre = "Margen Neto Bajo",
                Descripcion = "Empresas con margen neto menor a 2% requieren revisión",
                Campo = "MargenNeto",
                Operador = OperadorComparacion.MenorQue,
                Valor = 0.02m,
                Peso = 0.10m,
                Resultado = ResultadoRegla.Revisar,
                Activa = true,
                Orden = 7
            }
        };

        _context.ReglasEvaluacion.AddRange(reglas);
        _logger.LogInformation("Added {Count} reglas de evaluación", reglas.Count);
        return Task.CompletedTask;
    }
}