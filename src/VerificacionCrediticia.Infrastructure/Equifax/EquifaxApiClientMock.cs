using VerificacionCrediticia.Core.DTOs;
using VerificacionCrediticia.Core.Entities;
using VerificacionCrediticia.Core.Enums;
using VerificacionCrediticia.Core.Interfaces;

namespace VerificacionCrediticia.Infrastructure.Equifax;

/// <summary>
/// Cliente mock de Equifax para pruebas y desarrollo.
/// Simula la API real retornando un reporte crediticio completo por documento.
/// </summary>
public class EquifaxApiClientMock : IEquifaxApiClient
{
    private readonly Dictionary<string, ReporteCrediticioDto> _reportes;

    public EquifaxApiClientMock()
    {
        _reportes = InicializarReportes();
    }

    public async Task<ReporteCrediticioDto?> ConsultarReporteCrediticioAsync(
        string tipoDocumento,
        string numeroDocumento,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(100, cancellationToken);

        if (_reportes.TryGetValue(numeroDocumento, out var reporte))
        {
            return reporte;
        }

        // Documento no encontrado: retornar reporte generico
        if (tipoDocumento == "1") // DNI
        {
            return new ReporteCrediticioDto
            {
                TipoDocumento = "1",
                NumeroDocumento = numeroDocumento,
                DatosPersona = new DatosPersonaDto { Nombres = $"Persona No Registrada ({numeroDocumento})" },
                NivelRiesgoTexto = "RIESGO MODERADO",
                NivelRiesgo = NivelRiesgo.Moderado,
                Deudas = new List<DeudaRegistrada>()
            };
        }

        return new ReporteCrediticioDto
        {
            TipoDocumento = "6",
            NumeroDocumento = numeroDocumento,
            DatosEmpresa = new DatosEmpresaDto
            {
                RazonSocial = $"EMPRESA NO REGISTRADA ({numeroDocumento})",
                EstadoContribuyente = "NO HABIDO"
            },
            NivelRiesgoTexto = "RIESGO MODERADO",
            NivelRiesgo = NivelRiesgo.Moderado,
            Deudas = new List<DeudaRegistrada>()
        };
    }

    private static Dictionary<string, ReporteCrediticioDto> InicializarReportes()
    {
        var reportes = new Dictionary<string, ReporteCrediticioDto>();

        // ============================================================
        // CASO 1: Red limpia - Score bajo, todo en orden
        // DNI 12345678 / RUC 20123456789
        // ============================================================

        reportes["12345678"] = new ReporteCrediticioDto
        {
            TipoDocumento = "1",
            NumeroDocumento = "12345678",
            DatosPersona = new DatosPersonaDto
            {
                Nombres = "Juan Carlos Perez Garcia",
                FechaNacimiento = "15/03/1975",
                Nacionalidad = "PERU"
            },
            NivelRiesgoTexto = "RIESGO BAJO",
            NivelRiesgo = NivelRiesgo.Bajo,
            RepresentantesDe = new List<RepresentanteLegalDto>
            {
                new() { TipoDocumento = "6", NumeroDocumento = "20123456789", Nombre = "DISTRIBUIDORA COMERCIAL DEL NORTE SAC", Cargo = "Gerente General", FechaInicioCargo = "10/01/2015", NivelRiesgoTexto = "RIESGO BAJO", NivelRiesgo = NivelRiesgo.Bajo },
                new() { TipoDocumento = "6", NumeroDocumento = "20987654321", Nombre = "TECNOLOGIA AVANZADA PERU EIRL", Cargo = "Representante Legal", FechaInicioCargo = "05/06/2018", NivelRiesgoTexto = "RIESGO BAJO", NivelRiesgo = NivelRiesgo.Bajo }
            },
            Deudas = new List<DeudaRegistrada>
            {
                new() { Entidad = "BCP", TipoDeuda = "Tarjeta de Credito", MontoOriginal = 5000, SaldoActual = 2000, DiasVencidos = 0, Calificacion = "Normal" },
                new() { Entidad = "Interbank", TipoDeuda = "Prestamo Personal", MontoOriginal = 15000, SaldoActual = 8000, DiasVencidos = 0, Calificacion = "Normal" }
            }
        };

        reportes["87654321"] = new ReporteCrediticioDto
        {
            TipoDocumento = "1",
            NumeroDocumento = "87654321",
            DatosPersona = new DatosPersonaDto
            {
                Nombres = "Maria Elena Lopez Torres",
                FechaNacimiento = "22/07/1980",
                Nacionalidad = "PERU"
            },
            NivelRiesgoTexto = "RIESGO BAJO",
            NivelRiesgo = NivelRiesgo.Bajo,
            RepresentantesDe = new List<RepresentanteLegalDto>
            {
                new() { TipoDocumento = "6", NumeroDocumento = "20123456789", Nombre = "DISTRIBUIDORA COMERCIAL DEL NORTE SAC", Cargo = "Representante Legal", FechaInicioCargo = "10/01/2015", NivelRiesgoTexto = "RIESGO BAJO", NivelRiesgo = NivelRiesgo.Bajo },
                new() { TipoDocumento = "6", NumeroDocumento = "20987654321", Nombre = "TECNOLOGIA AVANZADA PERU EIRL", Cargo = "Gerente General", FechaInicioCargo = "01/03/2016", NivelRiesgoTexto = "RIESGO BAJO", NivelRiesgo = NivelRiesgo.Bajo }
            },
            Deudas = new List<DeudaRegistrada>
            {
                new() { Entidad = "BBVA", TipoDeuda = "Hipotecario", MontoOriginal = 200000, SaldoActual = 150000, DiasVencidos = 0, Calificacion = "Normal" }
            }
        };

        reportes["20123456789"] = new ReporteCrediticioDto
        {
            TipoDocumento = "6",
            NumeroDocumento = "20123456789",
            DatosEmpresa = new DatosEmpresaDto
            {
                RazonSocial = "DISTRIBUIDORA COMERCIAL DEL NORTE SAC",
                NombreComercial = "DICONOR",
                TipoContribuyente = "SOC.ANONIMA CERRADA",
                EstadoContribuyente = "ACTIVO",
                CondicionContribuyente = "HABIDO",
                InicioActividades = "10/01/2010"
            },
            NivelRiesgoTexto = "RIESGO BAJO",
            NivelRiesgo = NivelRiesgo.Bajo,
            RepresentadoPor = new List<RepresentanteLegalDto>
            {
                new() { TipoDocumento = "1", NumeroDocumento = "12345678", Nombre = "Juan Carlos Perez Garcia", Cargo = "Gerente General", FechaInicioCargo = "10/01/2015", NivelRiesgoTexto = "RIESGO BAJO", NivelRiesgo = NivelRiesgo.Bajo },
                new() { TipoDocumento = "1", NumeroDocumento = "87654321", Nombre = "Maria Elena Lopez Torres", Cargo = "Representante Legal", FechaInicioCargo = "10/01/2015", NivelRiesgoTexto = "RIESGO BAJO", NivelRiesgo = NivelRiesgo.Bajo }
            },
            EmpresasRelacionadas = new List<EmpresaRelacionadaDto>
            {
                new() { TipoDocumento = "6", NumeroDocumento = "20987654321", Nombre = "TECNOLOGIA AVANZADA PERU EIRL", Relacion = "Mismo Representante Legal", NivelRiesgoTexto = "RIESGO BAJO", NivelRiesgo = NivelRiesgo.Bajo }
            },
            Deudas = new List<DeudaRegistrada>
            {
                new() { Entidad = "BCP", TipoDeuda = "Linea de Credito", MontoOriginal = 100000, SaldoActual = 45000, DiasVencidos = 0, Calificacion = "Normal" },
                new() { Entidad = "BBVA", TipoDeuda = "Leasing", MontoOriginal = 80000, SaldoActual = 50000, DiasVencidos = 0, Calificacion = "Normal" }
            }
        };

        reportes["20987654321"] = new ReporteCrediticioDto
        {
            TipoDocumento = "6",
            NumeroDocumento = "20987654321",
            DatosEmpresa = new DatosEmpresaDto
            {
                RazonSocial = "TECNOLOGIA AVANZADA PERU EIRL",
                NombreComercial = "TECPERU",
                TipoContribuyente = "EMPRESA IND.RESP.LTDA",
                EstadoContribuyente = "ACTIVO",
                CondicionContribuyente = "HABIDO",
                InicioActividades = "01/03/2012"
            },
            NivelRiesgoTexto = "RIESGO BAJO",
            NivelRiesgo = NivelRiesgo.Bajo,
            RepresentadoPor = new List<RepresentanteLegalDto>
            {
                new() { TipoDocumento = "1", NumeroDocumento = "87654321", Nombre = "Maria Elena Lopez Torres", Cargo = "Gerente General", FechaInicioCargo = "01/03/2016", NivelRiesgoTexto = "RIESGO BAJO", NivelRiesgo = NivelRiesgo.Bajo },
                new() { TipoDocumento = "1", NumeroDocumento = "12345678", Nombre = "Juan Carlos Perez Garcia", Cargo = "Representante Legal", FechaInicioCargo = "05/06/2018", NivelRiesgoTexto = "RIESGO BAJO", NivelRiesgo = NivelRiesgo.Bajo }
            },
            EmpresasRelacionadas = new List<EmpresaRelacionadaDto>
            {
                new() { TipoDocumento = "6", NumeroDocumento = "20123456789", Nombre = "DISTRIBUIDORA COMERCIAL DEL NORTE SAC", Relacion = "Mismo Representante Legal", NivelRiesgoTexto = "RIESGO BAJO", NivelRiesgo = NivelRiesgo.Bajo }
            },
            Deudas = new List<DeudaRegistrada>
            {
                new() { Entidad = "Interbank", TipoDeuda = "Capital de Trabajo", MontoOriginal = 200000, SaldoActual = 120000, DiasVencidos = 0, Calificacion = "Normal" }
            }
        };

        // ============================================================
        // CASO 2: Solicitante moroso
        // DNI 44444444 / RUC 20111111111
        // ============================================================

        reportes["44444444"] = new ReporteCrediticioDto
        {
            TipoDocumento = "1",
            NumeroDocumento = "44444444",
            DatosPersona = new DatosPersonaDto
            {
                Nombres = "Luis Fernando Castro Vega",
                FechaNacimiento = "10/11/1968",
                Nacionalidad = "PERU"
            },
            NivelRiesgoTexto = "RIESGO ALTO",
            NivelRiesgo = NivelRiesgo.Alto,
            RepresentantesDe = new List<RepresentanteLegalDto>
            {
                new() { TipoDocumento = "6", NumeroDocumento = "20111111111", Nombre = "IMPORTACIONES GLOBALES SAC", Cargo = "Gerente General", FechaInicioCargo = "15/02/2012", NivelRiesgoTexto = "RIESGO ALTO", NivelRiesgo = NivelRiesgo.Alto },
                new() { TipoDocumento = "6", NumeroDocumento = "20333333333", Nombre = "SERVICIOS LOGISTICOS EXPRESS SAC", Cargo = "Representante Legal", FechaInicioCargo = "20/08/2019", NivelRiesgoTexto = "RIESGO BAJO", NivelRiesgo = NivelRiesgo.Bajo }
            },
            Deudas = new List<DeudaRegistrada>
            {
                new() { Entidad = "Scotiabank", TipoDeuda = "Tarjeta de Credito", MontoOriginal = 8000, SaldoActual = 12000, DiasVencidos = 95, Calificacion = "Deficiente", FechaVencimiento = DateTime.Now.AddDays(-95) },
                new() { Entidad = "Financiera Oh!", TipoDeuda = "Credito Consumo", MontoOriginal = 3000, SaldoActual = 4500, DiasVencidos = 45, Calificacion = "CPP", FechaVencimiento = DateTime.Now.AddDays(-45) }
            }
        };

        reportes["20111111111"] = new ReporteCrediticioDto
        {
            TipoDocumento = "6",
            NumeroDocumento = "20111111111",
            DatosEmpresa = new DatosEmpresaDto
            {
                RazonSocial = "IMPORTACIONES GLOBALES SAC",
                NombreComercial = "IMPOGLOBAL",
                TipoContribuyente = "SOC.ANONIMA CERRADA",
                EstadoContribuyente = "ACTIVO",
                CondicionContribuyente = "HABIDO"
            },
            NivelRiesgoTexto = "RIESGO ALTO",
            NivelRiesgo = NivelRiesgo.Alto,
            RepresentadoPor = new List<RepresentanteLegalDto>
            {
                new() { TipoDocumento = "1", NumeroDocumento = "44444444", Nombre = "Luis Fernando Castro Vega", Cargo = "Gerente General", FechaInicioCargo = "15/02/2012", NivelRiesgoTexto = "RIESGO ALTO", NivelRiesgo = NivelRiesgo.Alto },
                new() { TipoDocumento = "1", NumeroDocumento = "22222222", Nombre = "Ana Patricia Fernandez Ruiz", Cargo = "Representante Legal", FechaInicioCargo = "10/05/2015", NivelRiesgoTexto = "RIESGO MODERADO", NivelRiesgo = NivelRiesgo.Moderado }
            },
            Deudas = new List<DeudaRegistrada>
            {
                new() { Entidad = "Scotiabank", TipoDeuda = "Linea de Credito", MontoOriginal = 150000, SaldoActual = 180000, DiasVencidos = 60, Calificacion = "Deficiente", FechaVencimiento = DateTime.Now.AddDays(-60) },
                new() { Entidad = "SUNAT", TipoDeuda = "Deuda Tributaria", MontoOriginal = 45000, SaldoActual = 52000, DiasVencidos = 90, Calificacion = "Dudoso", FechaVencimiento = DateTime.Now.AddDays(-90) }
            }
        };

        reportes["22222222"] = new ReporteCrediticioDto
        {
            TipoDocumento = "1",
            NumeroDocumento = "22222222",
            DatosPersona = new DatosPersonaDto
            {
                Nombres = "Ana Patricia Fernandez Ruiz",
                FechaNacimiento = "28/04/1982",
                Nacionalidad = "PERU"
            },
            NivelRiesgoTexto = "RIESGO MODERADO",
            NivelRiesgo = NivelRiesgo.Moderado,
            RepresentantesDe = new List<RepresentanteLegalDto>
            {
                new() { TipoDocumento = "6", NumeroDocumento = "20111111111", Nombre = "IMPORTACIONES GLOBALES SAC", Cargo = "Representante Legal", FechaInicioCargo = "10/05/2015", NivelRiesgoTexto = "RIESGO ALTO", NivelRiesgo = NivelRiesgo.Alto }
            },
            Deudas = new List<DeudaRegistrada>
            {
                new() { Entidad = "Mibanco", TipoDeuda = "Microcredito", MontoOriginal = 10000, SaldoActual = 6000, DiasVencidos = 15, Calificacion = "CPP", FechaVencimiento = DateTime.Now.AddDays(-15) }
            }
        };

        reportes["20333333333"] = new ReporteCrediticioDto
        {
            TipoDocumento = "6",
            NumeroDocumento = "20333333333",
            DatosEmpresa = new DatosEmpresaDto
            {
                RazonSocial = "SERVICIOS LOGISTICOS EXPRESS SAC",
                NombreComercial = "SERVILOG",
                TipoContribuyente = "SOC.ANONIMA CERRADA",
                EstadoContribuyente = "ACTIVO",
                CondicionContribuyente = "HABIDO"
            },
            NivelRiesgoTexto = "RIESGO BAJO",
            NivelRiesgo = NivelRiesgo.Bajo,
            RepresentadoPor = new List<RepresentanteLegalDto>
            {
                new() { TipoDocumento = "1", NumeroDocumento = "44444444", Nombre = "Luis Fernando Castro Vega", Cargo = "Representante Legal", FechaInicioCargo = "20/08/2019", NivelRiesgoTexto = "RIESGO ALTO", NivelRiesgo = NivelRiesgo.Alto }
            },
            Deudas = new List<DeudaRegistrada>
            {
                new() { Entidad = "BBVA", TipoDeuda = "Factoring", MontoOriginal = 75000, SaldoActual = 25000, DiasVencidos = 0, Calificacion = "Normal" }
            }
        };

        // ============================================================
        // CASO 3: Empresa castigada
        // DNI 55555555 / RUC 20222222222
        // ============================================================

        reportes["55555555"] = new ReporteCrediticioDto
        {
            TipoDocumento = "1",
            NumeroDocumento = "55555555",
            DatosPersona = new DatosPersonaDto
            {
                Nombres = "Patricia Rojas Diaz",
                FechaNacimiento = "03/09/1977",
                Nacionalidad = "PERU"
            },
            NivelRiesgoTexto = "RIESGO MODERADO",
            NivelRiesgo = NivelRiesgo.Moderado,
            RepresentantesDe = new List<RepresentanteLegalDto>
            {
                new() { TipoDocumento = "6", NumeroDocumento = "20222222222", Nombre = "CONSTRUCTORA ANDES SRL", Cargo = "Representante Legal", FechaInicioCargo = "01/01/2014", NivelRiesgoTexto = "RIESGO MUY ALTO", NivelRiesgo = NivelRiesgo.MuyAlto }
            },
            Deudas = new List<DeudaRegistrada>
            {
                new() { Entidad = "Caja Arequipa", TipoDeuda = "Credito PYME", MontoOriginal = 50000, SaldoActual = 30000, DiasVencidos = 0, Calificacion = "Normal" }
            }
        };

        reportes["20222222222"] = new ReporteCrediticioDto
        {
            TipoDocumento = "6",
            NumeroDocumento = "20222222222",
            DatosEmpresa = new DatosEmpresaDto
            {
                RazonSocial = "CONSTRUCTORA ANDES SRL",
                NombreComercial = "CONANDES",
                TipoContribuyente = "SOC.COM.RESPONS.LTDA",
                EstadoContribuyente = "BAJA",
                CondicionContribuyente = "NO HABIDO"
            },
            NivelRiesgoTexto = "RIESGO MUY ALTO",
            NivelRiesgo = NivelRiesgo.MuyAlto,
            RepresentadoPor = new List<RepresentanteLegalDto>
            {
                new() { TipoDocumento = "1", NumeroDocumento = "55555555", Nombre = "Patricia Rojas Diaz", Cargo = "Representante Legal", FechaInicioCargo = "01/01/2014", NivelRiesgoTexto = "RIESGO MODERADO", NivelRiesgo = NivelRiesgo.Moderado },
                new() { TipoDocumento = "1", NumeroDocumento = "33333333", Nombre = "Carlos Alberto Mendoza Quispe", Cargo = "Gerente General", FechaInicioCargo = "01/01/2010", NivelRiesgoTexto = "RIESGO MUY ALTO", NivelRiesgo = NivelRiesgo.MuyAlto }
            },
            Deudas = new List<DeudaRegistrada>
            {
                new() { Entidad = "BCP", TipoDeuda = "Prestamo Comercial", MontoOriginal = 500000, SaldoActual = 650000, DiasVencidos = 365, Calificacion = "Perdida", FechaVencimiento = DateTime.Now.AddDays(-365) }
            }
        };

        reportes["33333333"] = new ReporteCrediticioDto
        {
            TipoDocumento = "1",
            NumeroDocumento = "33333333",
            DatosPersona = new DatosPersonaDto
            {
                Nombres = "Carlos Alberto Mendoza Quispe",
                FechaNacimiento = "12/12/1965"
            },
            NivelRiesgoTexto = "RIESGO MUY ALTO",
            NivelRiesgo = NivelRiesgo.MuyAlto,
            RepresentantesDe = new List<RepresentanteLegalDto>
            {
                new() { TipoDocumento = "6", NumeroDocumento = "20222222222", Nombre = "CONSTRUCTORA ANDES SRL", Cargo = "Gerente General", FechaInicioCargo = "01/01/2010", NivelRiesgoTexto = "RIESGO MUY ALTO", NivelRiesgo = NivelRiesgo.MuyAlto }
            },
            Deudas = new List<DeudaRegistrada>
            {
                new() { Entidad = "BCP", TipoDeuda = "Prestamo Personal", MontoOriginal = 25000, SaldoActual = 35000, DiasVencidos = 180, Calificacion = "Perdida", FechaVencimiento = DateTime.Now.AddDays(-180) }
            }
        };

        // ============================================================
        // CASO 4: Red con representantes problematicos en 2do nivel
        // DNI 66666666 / RUC 20444444444
        // Solicitante y empresa limpios, pero representantes de la
        // empresa tienen riesgo alto y muy alto
        // ============================================================

        reportes["66666666"] = new ReporteCrediticioDto
        {
            TipoDocumento = "1",
            NumeroDocumento = "66666666",
            DatosPersona = new DatosPersonaDto
            {
                Nombres = "Ricardo Vargas Mendoza",
                FechaNacimiento = "18/05/1972",
                Nacionalidad = "PERU"
            },
            NivelRiesgoTexto = "RIESGO BAJO",
            NivelRiesgo = NivelRiesgo.Bajo,
            RepresentantesDe = new List<RepresentanteLegalDto>
            {
                new() { TipoDocumento = "6", NumeroDocumento = "20444444444", Nombre = "INVERSIONES ANDINAS SAC", Cargo = "Gerente General", FechaInicioCargo = "01/06/2016", NivelRiesgoTexto = "RIESGO BAJO", NivelRiesgo = NivelRiesgo.Bajo }
            },
            Deudas = new List<DeudaRegistrada>
            {
                new() { Entidad = "BCP", TipoDeuda = "Tarjeta de Credito", MontoOriginal = 10000, SaldoActual = 2000, DiasVencidos = 0, Calificacion = "Normal" }
            }
        };

        reportes["20444444444"] = new ReporteCrediticioDto
        {
            TipoDocumento = "6",
            NumeroDocumento = "20444444444",
            DatosEmpresa = new DatosEmpresaDto
            {
                RazonSocial = "INVERSIONES ANDINAS SAC",
                NombreComercial = "INVANDINAS",
                TipoContribuyente = "SOC.ANONIMA CERRADA",
                EstadoContribuyente = "ACTIVO",
                CondicionContribuyente = "HABIDO",
                InicioActividades = "01/06/2010"
            },
            NivelRiesgoTexto = "RIESGO BAJO",
            NivelRiesgo = NivelRiesgo.Bajo,
            RepresentadoPor = new List<RepresentanteLegalDto>
            {
                new() { TipoDocumento = "1", NumeroDocumento = "66666666", Nombre = "Ricardo Vargas Mendoza", Cargo = "Gerente General", FechaInicioCargo = "01/06/2016", NivelRiesgoTexto = "RIESGO BAJO", NivelRiesgo = NivelRiesgo.Bajo },
                new() { TipoDocumento = "1", NumeroDocumento = "77777777", Nombre = "Eduardo Quispe Mamani", Cargo = "Representante Legal", FechaInicioCargo = "15/03/2017", NivelRiesgoTexto = "RIESGO ALTO", NivelRiesgo = NivelRiesgo.Alto },
                new() { TipoDocumento = "1", NumeroDocumento = "88888888", Nombre = "Carmen Flores Huanca", Cargo = "Representante Legal", FechaInicioCargo = "20/09/2018", NivelRiesgoTexto = "RIESGO MUY ALTO", NivelRiesgo = NivelRiesgo.MuyAlto }
            },
            Deudas = new List<DeudaRegistrada>
            {
                new() { Entidad = "BCP", TipoDeuda = "Linea de Credito", MontoOriginal = 200000, SaldoActual = 50000, DiasVencidos = 0, Calificacion = "Normal" },
                new() { Entidad = "Interbank", TipoDeuda = "Leasing Vehicular", MontoOriginal = 120000, SaldoActual = 80000, DiasVencidos = 0, Calificacion = "Normal" }
            }
        };

        reportes["77777777"] = new ReporteCrediticioDto
        {
            TipoDocumento = "1",
            NumeroDocumento = "77777777",
            DatosPersona = new DatosPersonaDto
            {
                Nombres = "Eduardo Quispe Mamani",
                FechaNacimiento = "25/01/1985"
            },
            NivelRiesgoTexto = "RIESGO ALTO",
            NivelRiesgo = NivelRiesgo.Alto,
            RepresentantesDe = new List<RepresentanteLegalDto>
            {
                new() { TipoDocumento = "6", NumeroDocumento = "20444444444", Nombre = "INVERSIONES ANDINAS SAC", Cargo = "Representante Legal", FechaInicioCargo = "15/03/2017", NivelRiesgoTexto = "RIESGO BAJO", NivelRiesgo = NivelRiesgo.Bajo }
            },
            Deudas = new List<DeudaRegistrada>
            {
                new() { Entidad = "Scotiabank", TipoDeuda = "Prestamo Personal", MontoOriginal = 30000, SaldoActual = 45000, DiasVencidos = 120, Calificacion = "Dudoso", FechaVencimiento = DateTime.Now.AddDays(-120) },
                new() { Entidad = "Financiera Crediscotia", TipoDeuda = "Tarjeta de Credito", MontoOriginal = 8000, SaldoActual = 15000, DiasVencidos = 90, Calificacion = "Deficiente", FechaVencimiento = DateTime.Now.AddDays(-90) }
            }
        };

        reportes["88888888"] = new ReporteCrediticioDto
        {
            TipoDocumento = "1",
            NumeroDocumento = "88888888",
            DatosPersona = new DatosPersonaDto
            {
                Nombres = "Carmen Flores Huanca",
                FechaNacimiento = "07/08/1978"
            },
            NivelRiesgoTexto = "RIESGO MUY ALTO",
            NivelRiesgo = NivelRiesgo.MuyAlto,
            RepresentantesDe = new List<RepresentanteLegalDto>
            {
                new() { TipoDocumento = "6", NumeroDocumento = "20444444444", Nombre = "INVERSIONES ANDINAS SAC", Cargo = "Representante Legal", FechaInicioCargo = "20/09/2018", NivelRiesgoTexto = "RIESGO BAJO", NivelRiesgo = NivelRiesgo.Bajo }
            },
            Deudas = new List<DeudaRegistrada>
            {
                new() { Entidad = "BBVA", TipoDeuda = "Prestamo Comercial", MontoOriginal = 50000, SaldoActual = 75000, DiasVencidos = 200, Calificacion = "Perdida", FechaVencimiento = DateTime.Now.AddDays(-200) }
            }
        };

        // ============================================================
        // CASO 5: Cadena de 3 niveles de profundidad
        // DNI 99999999 / RUC 20555555555
        // Nivel 0: Alberto (limpio) + CONSURSUR (limpia)
        // Nivel 1: Fernando (limpio) + DISPACIF (limpia)
        // Nivel 2: Gabriela (problemas) + MINERALTI (problemas)
        // Nivel 3: Hector (castigado) + Isabel (morosa)
        // ============================================================

        reportes["99999999"] = new ReporteCrediticioDto
        {
            TipoDocumento = "1",
            NumeroDocumento = "99999999",
            DatosPersona = new DatosPersonaDto
            {
                Nombres = "Alberto Ramirez Soto",
                FechaNacimiento = "20/02/1970",
                Nacionalidad = "PERU"
            },
            NivelRiesgoTexto = "RIESGO BAJO",
            NivelRiesgo = NivelRiesgo.Bajo,
            RepresentantesDe = new List<RepresentanteLegalDto>
            {
                new() { TipoDocumento = "6", NumeroDocumento = "20555555555", Nombre = "CONSULTORES ASOCIADOS DEL SUR SAC", Cargo = "Gerente General", FechaInicioCargo = "01/01/2012", NivelRiesgoTexto = "RIESGO BAJO", NivelRiesgo = NivelRiesgo.Bajo }
            },
            Deudas = new List<DeudaRegistrada>
            {
                new() { Entidad = "BCP", TipoDeuda = "Tarjeta de Credito", MontoOriginal = 15000, SaldoActual = 3000, DiasVencidos = 0, Calificacion = "Normal" }
            }
        };

        reportes["20555555555"] = new ReporteCrediticioDto
        {
            TipoDocumento = "6",
            NumeroDocumento = "20555555555",
            DatosEmpresa = new DatosEmpresaDto
            {
                RazonSocial = "CONSULTORES ASOCIADOS DEL SUR SAC",
                NombreComercial = "CONSURSUR",
                TipoContribuyente = "SOC.ANONIMA CERRADA",
                EstadoContribuyente = "ACTIVO",
                CondicionContribuyente = "HABIDO",
                InicioActividades = "01/01/2008"
            },
            NivelRiesgoTexto = "RIESGO BAJO",
            NivelRiesgo = NivelRiesgo.Bajo,
            RepresentadoPor = new List<RepresentanteLegalDto>
            {
                new() { TipoDocumento = "1", NumeroDocumento = "99999999", Nombre = "Alberto Ramirez Soto", Cargo = "Gerente General", FechaInicioCargo = "01/01/2012", NivelRiesgoTexto = "RIESGO BAJO", NivelRiesgo = NivelRiesgo.Bajo },
                new() { TipoDocumento = "1", NumeroDocumento = "10101010", Nombre = "Fernando Gutierrez Palacios", Cargo = "Representante Legal", FechaInicioCargo = "15/06/2014", NivelRiesgoTexto = "RIESGO BAJO", NivelRiesgo = NivelRiesgo.Bajo }
            },
            Deudas = new List<DeudaRegistrada>
            {
                new() { Entidad = "BCP", TipoDeuda = "Linea de Credito", MontoOriginal = 300000, SaldoActual = 80000, DiasVencidos = 0, Calificacion = "Normal" }
            }
        };

        // Nivel 1
        reportes["10101010"] = new ReporteCrediticioDto
        {
            TipoDocumento = "1",
            NumeroDocumento = "10101010",
            DatosPersona = new DatosPersonaDto
            {
                Nombres = "Fernando Gutierrez Palacios",
                FechaNacimiento = "11/11/1980"
            },
            NivelRiesgoTexto = "RIESGO BAJO",
            NivelRiesgo = NivelRiesgo.Bajo,
            RepresentantesDe = new List<RepresentanteLegalDto>
            {
                new() { TipoDocumento = "6", NumeroDocumento = "20555555555", Nombre = "CONSULTORES ASOCIADOS DEL SUR SAC", Cargo = "Representante Legal", FechaInicioCargo = "15/06/2014", NivelRiesgoTexto = "RIESGO BAJO", NivelRiesgo = NivelRiesgo.Bajo },
                new() { TipoDocumento = "6", NumeroDocumento = "20666666666", Nombre = "DISTRIBUCIONES PACIFICO EIRL", Cargo = "Gerente General", FechaInicioCargo = "01/03/2016", NivelRiesgoTexto = "RIESGO BAJO", NivelRiesgo = NivelRiesgo.Bajo }
            },
            Deudas = new List<DeudaRegistrada>
            {
                new() { Entidad = "Interbank", TipoDeuda = "Prestamo Personal", MontoOriginal = 20000, SaldoActual = 10000, DiasVencidos = 0, Calificacion = "Normal" }
            }
        };

        reportes["20666666666"] = new ReporteCrediticioDto
        {
            TipoDocumento = "6",
            NumeroDocumento = "20666666666",
            DatosEmpresa = new DatosEmpresaDto
            {
                RazonSocial = "DISTRIBUCIONES PACIFICO EIRL",
                NombreComercial = "DISPACIF",
                TipoContribuyente = "EMPRESA IND.RESP.LTDA",
                EstadoContribuyente = "ACTIVO",
                CondicionContribuyente = "HABIDO"
            },
            NivelRiesgoTexto = "RIESGO BAJO",
            NivelRiesgo = NivelRiesgo.Bajo,
            RepresentadoPor = new List<RepresentanteLegalDto>
            {
                new() { TipoDocumento = "1", NumeroDocumento = "10101010", Nombre = "Fernando Gutierrez Palacios", Cargo = "Gerente General", FechaInicioCargo = "01/03/2016", NivelRiesgoTexto = "RIESGO BAJO", NivelRiesgo = NivelRiesgo.Bajo },
                new() { TipoDocumento = "1", NumeroDocumento = "20202020", Nombre = "Gabriela Torres Medina", Cargo = "Representante Legal", FechaInicioCargo = "10/01/2018", NivelRiesgoTexto = "RIESGO MODERADO", NivelRiesgo = NivelRiesgo.Moderado }
            },
            Deudas = new List<DeudaRegistrada>
            {
                new() { Entidad = "BBVA", TipoDeuda = "Capital de Trabajo", MontoOriginal = 150000, SaldoActual = 90000, DiasVencidos = 0, Calificacion = "Normal" }
            }
        };

        // Nivel 2
        reportes["20202020"] = new ReporteCrediticioDto
        {
            TipoDocumento = "1",
            NumeroDocumento = "20202020",
            DatosPersona = new DatosPersonaDto
            {
                Nombres = "Gabriela Torres Medina",
                FechaNacimiento = "14/06/1983"
            },
            NivelRiesgoTexto = "RIESGO MODERADO",
            NivelRiesgo = NivelRiesgo.Moderado,
            RepresentantesDe = new List<RepresentanteLegalDto>
            {
                new() { TipoDocumento = "6", NumeroDocumento = "20666666666", Nombre = "DISTRIBUCIONES PACIFICO EIRL", Cargo = "Representante Legal", FechaInicioCargo = "10/01/2018", NivelRiesgoTexto = "RIESGO BAJO", NivelRiesgo = NivelRiesgo.Bajo },
                new() { TipoDocumento = "6", NumeroDocumento = "20777777777", Nombre = "MINERA ALTIPLANO SAC", Cargo = "Representante Legal", FechaInicioCargo = "05/05/2019", NivelRiesgoTexto = "RIESGO MODERADO", NivelRiesgo = NivelRiesgo.Moderado }
            },
            Deudas = new List<DeudaRegistrada>
            {
                new() { Entidad = "BBVA", TipoDeuda = "Tarjeta de Credito", MontoOriginal = 12000, SaldoActual = 8000, DiasVencidos = 20, Calificacion = "CPP", FechaVencimiento = DateTime.Now.AddDays(-20) }
            }
        };

        reportes["20777777777"] = new ReporteCrediticioDto
        {
            TipoDocumento = "6",
            NumeroDocumento = "20777777777",
            DatosEmpresa = new DatosEmpresaDto
            {
                RazonSocial = "MINERA ALTIPLANO SAC",
                NombreComercial = "MINERALTI",
                TipoContribuyente = "SOC.ANONIMA CERRADA",
                EstadoContribuyente = "ACTIVO",
                CondicionContribuyente = "HABIDO"
            },
            NivelRiesgoTexto = "RIESGO MODERADO",
            NivelRiesgo = NivelRiesgo.Moderado,
            RepresentadoPor = new List<RepresentanteLegalDto>
            {
                new() { TipoDocumento = "1", NumeroDocumento = "20202020", Nombre = "Gabriela Torres Medina", Cargo = "Representante Legal", FechaInicioCargo = "05/05/2019", NivelRiesgoTexto = "RIESGO MODERADO", NivelRiesgo = NivelRiesgo.Moderado },
                new() { TipoDocumento = "1", NumeroDocumento = "30303030", Nombre = "Hector Villanueva Cruz", Cargo = "Gerente General", FechaInicioCargo = "01/01/2017", NivelRiesgoTexto = "RIESGO MUY ALTO", NivelRiesgo = NivelRiesgo.MuyAlto },
                new() { TipoDocumento = "1", NumeroDocumento = "40404040", Nombre = "Isabel Chavez Rios", Cargo = "Representante Legal", FechaInicioCargo = "15/08/2020", NivelRiesgoTexto = "RIESGO ALTO", NivelRiesgo = NivelRiesgo.Alto }
            },
            Deudas = new List<DeudaRegistrada>
            {
                new() { Entidad = "Scotiabank", TipoDeuda = "Prestamo Comercial", MontoOriginal = 500000, SaldoActual = 350000, DiasVencidos = 30, Calificacion = "CPP", FechaVencimiento = DateTime.Now.AddDays(-30) }
            }
        };

        // Nivel 3 - Los problematicos
        reportes["30303030"] = new ReporteCrediticioDto
        {
            TipoDocumento = "1",
            NumeroDocumento = "30303030",
            DatosPersona = new DatosPersonaDto
            {
                Nombres = "Hector Villanueva Cruz",
                FechaNacimiento = "30/10/1960"
            },
            NivelRiesgoTexto = "RIESGO MUY ALTO",
            NivelRiesgo = NivelRiesgo.MuyAlto,
            RepresentantesDe = new List<RepresentanteLegalDto>
            {
                new() { TipoDocumento = "6", NumeroDocumento = "20777777777", Nombre = "MINERA ALTIPLANO SAC", Cargo = "Gerente General", FechaInicioCargo = "01/01/2017", NivelRiesgoTexto = "RIESGO MODERADO", NivelRiesgo = NivelRiesgo.Moderado }
            },
            Deudas = new List<DeudaRegistrada>
            {
                new() { Entidad = "Scotiabank", TipoDeuda = "Prestamo Comercial", MontoOriginal = 100000, SaldoActual = 150000, DiasVencidos = 300, Calificacion = "Perdida", FechaVencimiento = DateTime.Now.AddDays(-300) },
                new() { Entidad = "SUNAT", TipoDeuda = "Deuda Tributaria", MontoOriginal = 80000, SaldoActual = 95000, DiasVencidos = 240, Calificacion = "Perdida", FechaVencimiento = DateTime.Now.AddDays(-240) }
            }
        };

        reportes["40404040"] = new ReporteCrediticioDto
        {
            TipoDocumento = "1",
            NumeroDocumento = "40404040",
            DatosPersona = new DatosPersonaDto
            {
                Nombres = "Isabel Chavez Rios",
                FechaNacimiento = "09/04/1988"
            },
            NivelRiesgoTexto = "RIESGO ALTO",
            NivelRiesgo = NivelRiesgo.Alto,
            RepresentantesDe = new List<RepresentanteLegalDto>
            {
                new() { TipoDocumento = "6", NumeroDocumento = "20777777777", Nombre = "MINERA ALTIPLANO SAC", Cargo = "Representante Legal", FechaInicioCargo = "15/08/2020", NivelRiesgoTexto = "RIESGO MODERADO", NivelRiesgo = NivelRiesgo.Moderado }
            },
            Deudas = new List<DeudaRegistrada>
            {
                new() { Entidad = "BCP", TipoDeuda = "Linea de Credito", MontoOriginal = 40000, SaldoActual = 55000, DiasVencidos = 150, Calificacion = "Dudoso", FechaVencimiento = DateTime.Now.AddDays(-150) }
            }
        };

        return reportes;
    }
}
