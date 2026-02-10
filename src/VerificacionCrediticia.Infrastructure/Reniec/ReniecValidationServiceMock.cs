using Microsoft.Extensions.Logging;
using VerificacionCrediticia.Core.DTOs;
using VerificacionCrediticia.Core.Interfaces;

namespace VerificacionCrediticia.Infrastructure.Reniec;

/// <summary>
/// Mock del servicio RENIEC para desarrollo y pruebas.
/// Valida DNIs conocidos del sistema (mock Equifax + UAT Equifax).
/// </summary>
public class ReniecValidationServiceMock : IReniecValidationService
{
    private readonly ILogger<ReniecValidationServiceMock> _logger;
    private readonly Dictionary<string, (string Nombres, string Apellidos)> _dnisValidos;

    public ReniecValidationServiceMock(ILogger<ReniecValidationServiceMock> logger)
    {
        _logger = logger;
        _dnisValidos = InicializarDnisValidos();
    }

    public async Task<ReniecValidacionDto> ValidarDniAsync(
        string numeroDni,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(150, cancellationToken); // Simular latencia

        _logger.LogInformation("[MOCK RENIEC] Validando DNI: {Dni}", numeroDni);

        if (_dnisValidos.TryGetValue(numeroDni, out var persona))
        {
            _logger.LogInformation(
                "[MOCK RENIEC] DNI {Dni} valido: {Nombres} {Apellidos}",
                numeroDni, persona.Nombres, persona.Apellidos);

            return new ReniecValidacionDto
            {
                DniValido = true,
                Mensaje = "DNI validado por RENIEC",
                NombresReniec = persona.Nombres,
                ApellidosReniec = persona.Apellidos
            };
        }

        _logger.LogInformation("[MOCK RENIEC] DNI {Dni} no encontrado", numeroDni);

        return new ReniecValidacionDto
        {
            DniValido = false,
            Mensaje = "DNI no encontrado en RENIEC"
        };
    }

    private static Dictionary<string, (string Nombres, string Apellidos)> InicializarDnisValidos()
    {
        return new Dictionary<string, (string, string)>
        {
            // Mock Equifax - datos de prueba internos
            ["12345678"] = ("JUAN CARLOS", "PEREZ GARCIA"),
            ["87654321"] = ("MARIA ELENA", "LOPEZ TORRES"),
            ["44444444"] = ("LUIS FERNANDO", "CASTRO VEGA"),
            ["22222222"] = ("ANA PATRICIA", "FERNANDEZ RUIZ"),
            ["55555555"] = ("PATRICIA", "ROJAS DIAZ"),
            ["33333333"] = ("CARLOS ALBERTO", "MENDOZA QUISPE"),
            ["66666666"] = ("RICARDO", "VARGAS MENDOZA"),
            ["77777777"] = ("EDUARDO", "QUISPE MAMANI"),
            ["88888888"] = ("CARMEN", "FLORES HUANCA"),
            ["99999999"] = ("ALBERTO", "RAMIREZ SOTO"),
            ["10101010"] = ("FERNANDO", "GUTIERREZ PALACIOS"),
            ["20202020"] = ("GABRIELA", "TORRES MEDINA"),
            ["30303030"] = ("HECTOR", "VILLANUEVA CRUZ"),
            ["40404040"] = ("ISABEL", "CHAVEZ RIOS"),

            // DNIs de prueba reales
            ["46590189"] = ("AILEEN MEILYN", "LEI KOO"),

            // Caso 6: Aileen y socios
            ["51515151"] = ("DIEGO ARMANDO", "VARGAS RAMOS"),
            ["52525252"] = ("LUCIA ANDREA", "PAREDES SOTO"),
            ["53535353"] = ("RAUL ENRIQUE", "MONTOYA DIAZ"),
            ["54545454"] = ("OSCAR FAVIO", "HUAMAN RIOS"),
            ["55505050"] = ("VICTOR MANUEL", "QUISPE TORRES"),

            // DNIs morosos Equifax UAT
            ["41197536"] = ("CARLOS ENRIQUE", "MARTINEZ SALAZAR"),
            ["43109722"] = ("ROSA MARIA", "HUAMAN QUISPE"),
            ["16027546"] = ("JORGE LUIS", "GARCIA ROJAS"),
            ["04052936"] = ("PEDRO ANTONIO", "SILVA RAMOS"),
            ["02858608"] = ("MARIA LUISA", "FERNANDEZ VEGA"),

            // DNIs con info financiera Equifax UAT
            ["08587800"] = ("ROBERTO CARLOS", "TORRES LUNA"),
            ["06906697"] = ("ANA MARIA", "RODRIGUEZ DIAZ"),
            ["08551803"] = ("LUIS ALBERTO", "CASTILLO PONCE"),
            ["07679095"] = ("ELENA PATRICIA", "VARGAS RUIZ"),
            ["07725758"] = ("MIGUEL ANGEL", "SANTOS HERRERA"),
        };
    }
}
