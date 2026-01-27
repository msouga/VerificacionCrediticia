using VerificacionCrediticia.Core.Entities;
using VerificacionCrediticia.Core.Enums;
using VerificacionCrediticia.Core.Interfaces;

namespace VerificacionCrediticia.Infrastructure.Equifax;

/// <summary>
/// Cliente mock de Equifax para pruebas y desarrollo.
/// Simula respuestas de la API con datos de prueba.
/// </summary>
public class EquifaxApiClientMock : IEquifaxApiClient
{
    // Base de datos simulada de personas
    private readonly Dictionary<string, Persona> _personas = new()
    {
        ["12345678"] = new Persona
        {
            Dni = "12345678",
            Nombres = "Juan Carlos",
            Apellidos = "Pérez García",
            ScoreCrediticio = 750,
            Estado = EstadoCrediticio.Normal,
            Deudas = new List<DeudaRegistrada>
            {
                new() { Entidad = "BCP", TipoDeuda = "Tarjeta de Crédito", MontoOriginal = 5000, SaldoActual = 2000, DiasVencidos = 0, Calificacion = "Normal" },
                new() { Entidad = "Interbank", TipoDeuda = "Préstamo Personal", MontoOriginal = 15000, SaldoActual = 8000, DiasVencidos = 0, Calificacion = "Normal" }
            }
        },
        ["87654321"] = new Persona
        {
            Dni = "87654321",
            Nombres = "María Elena",
            Apellidos = "López Torres",
            ScoreCrediticio = 680,
            Estado = EstadoCrediticio.Normal,
            Deudas = new List<DeudaRegistrada>
            {
                new() { Entidad = "BBVA", TipoDeuda = "Hipotecario", MontoOriginal = 200000, SaldoActual = 150000, DiasVencidos = 0, Calificacion = "Normal" }
            }
        },
        ["11111111"] = new Persona
        {
            Dni = "11111111",
            Nombres = "Roberto",
            Apellidos = "Sánchez Medina",
            ScoreCrediticio = 420,
            Estado = EstadoCrediticio.Moroso,
            Deudas = new List<DeudaRegistrada>
            {
                new() { Entidad = "Scotiabank", TipoDeuda = "Tarjeta de Crédito", MontoOriginal = 8000, SaldoActual = 12000, DiasVencidos = 95, Calificacion = "Deficiente", FechaVencimiento = DateTime.Now.AddDays(-95) },
                new() { Entidad = "Financiera Oh!", TipoDeuda = "Crédito Consumo", MontoOriginal = 3000, SaldoActual = 4500, DiasVencidos = 45, Calificacion = "CPP", FechaVencimiento = DateTime.Now.AddDays(-45) }
            }
        },
        ["22222222"] = new Persona
        {
            Dni = "22222222",
            Nombres = "Ana Patricia",
            Apellidos = "Fernández Ruiz",
            ScoreCrediticio = 580,
            Estado = EstadoCrediticio.ConProblemasPotenciales,
            Deudas = new List<DeudaRegistrada>
            {
                new() { Entidad = "Mibanco", TipoDeuda = "Microcrédito", MontoOriginal = 10000, SaldoActual = 6000, DiasVencidos = 15, Calificacion = "CPP", FechaVencimiento = DateTime.Now.AddDays(-15) }
            }
        },
        ["33333333"] = new Persona
        {
            Dni = "33333333",
            Nombres = "Carlos Alberto",
            Apellidos = "Mendoza Quispe",
            ScoreCrediticio = 290,
            Estado = EstadoCrediticio.Castigado,
            Deudas = new List<DeudaRegistrada>
            {
                new() { Entidad = "BCP", TipoDeuda = "Préstamo Personal", MontoOriginal = 25000, SaldoActual = 35000, DiasVencidos = 180, Calificacion = "Pérdida", FechaVencimiento = DateTime.Now.AddDays(-180) }
            }
        },
        ["44444444"] = new Persona
        {
            Dni = "44444444",
            Nombres = "Luis Fernando",
            Apellidos = "Castro Vega",
            ScoreCrediticio = 710,
            Estado = EstadoCrediticio.Normal,
            Deudas = new List<DeudaRegistrada>()
        },
        ["55555555"] = new Persona
        {
            Dni = "55555555",
            Nombres = "Patricia",
            Apellidos = "Rojas Díaz",
            ScoreCrediticio = 620,
            Estado = EstadoCrediticio.Normal,
            Deudas = new List<DeudaRegistrada>
            {
                new() { Entidad = "Caja Arequipa", TipoDeuda = "Crédito PYME", MontoOriginal = 50000, SaldoActual = 30000, DiasVencidos = 0, Calificacion = "Normal" }
            }
        },
        // CASO ESPECIAL: Solicitante limpio, empresa limpia, pero relaciones de 2do nivel problematicas
        ["66666666"] = new Persona
        {
            Dni = "66666666",
            Nombres = "Ricardo",
            Apellidos = "Vargas Mendoza",
            ScoreCrediticio = 780,
            Estado = EstadoCrediticio.Normal,
            Deudas = new List<DeudaRegistrada>
            {
                new() { Entidad = "BCP", TipoDeuda = "Tarjeta de Crédito", MontoOriginal = 10000, SaldoActual = 2000, DiasVencidos = 0, Calificacion = "Normal" }
            }
        },
        // Socio problematico de segundo nivel (socio de la empresa donde Ricardo es socio)
        ["77777777"] = new Persona
        {
            Dni = "77777777",
            Nombres = "Eduardo",
            Apellidos = "Quispe Mamani",
            ScoreCrediticio = 320,
            Estado = EstadoCrediticio.Moroso,
            Deudas = new List<DeudaRegistrada>
            {
                new() { Entidad = "Scotiabank", TipoDeuda = "Préstamo Personal", MontoOriginal = 30000, SaldoActual = 45000, DiasVencidos = 120, Calificacion = "Dudoso", FechaVencimiento = DateTime.Now.AddDays(-120) },
                new() { Entidad = "Financiera Crediscotia", TipoDeuda = "Tarjeta de Crédito", MontoOriginal = 8000, SaldoActual = 15000, DiasVencidos = 90, Calificacion = "Deficiente", FechaVencimiento = DateTime.Now.AddDays(-90) }
            }
        },
        // Otro socio problematico de segundo nivel
        ["88888888"] = new Persona
        {
            Dni = "88888888",
            Nombres = "Carmen",
            Apellidos = "Flores Huanca",
            ScoreCrediticio = 250,
            Estado = EstadoCrediticio.Castigado,
            Deudas = new List<DeudaRegistrada>
            {
                new() { Entidad = "BBVA", TipoDeuda = "Préstamo Comercial", MontoOriginal = 50000, SaldoActual = 75000, DiasVencidos = 200, Calificacion = "Pérdida", FechaVencimiento = DateTime.Now.AddDays(-200) }
            }
        },

        // === CASO RED PROFUNDA (3 niveles) ===
        // Nivel 0: Solicitante limpio
        ["99999999"] = new Persona
        {
            Dni = "99999999",
            Nombres = "Alberto",
            Apellidos = "Ramirez Soto",
            ScoreCrediticio = 800,
            Estado = EstadoCrediticio.Normal,
            Deudas = new List<DeudaRegistrada>
            {
                new() { Entidad = "BCP", TipoDeuda = "Tarjeta de Crédito", MontoOriginal = 15000, SaldoActual = 3000, DiasVencidos = 0, Calificacion = "Normal" }
            }
        },
        // Nivel 1: Socio limpio de la empresa principal, tambien socio de DISTRIBUCIONES PACIFICO
        ["10101010"] = new Persona
        {
            Dni = "10101010",
            Nombres = "Fernando",
            Apellidos = "Gutierrez Palacios",
            ScoreCrediticio = 720,
            Estado = EstadoCrediticio.Normal,
            Deudas = new List<DeudaRegistrada>
            {
                new() { Entidad = "Interbank", TipoDeuda = "Préstamo Personal", MontoOriginal = 20000, SaldoActual = 10000, DiasVencidos = 0, Calificacion = "Normal" }
            }
        },
        // Nivel 2: Socio de DISTRIBUCIONES PACIFICO, tambien socio de MINERA ALTIPLANO
        ["20202020"] = new Persona
        {
            Dni = "20202020",
            Nombres = "Gabriela",
            Apellidos = "Torres Medina",
            ScoreCrediticio = 650,
            Estado = EstadoCrediticio.ConProblemasPotenciales,
            Deudas = new List<DeudaRegistrada>
            {
                new() { Entidad = "BBVA", TipoDeuda = "Tarjeta de Crédito", MontoOriginal = 12000, SaldoActual = 8000, DiasVencidos = 20, Calificacion = "CPP", FechaVencimiento = DateTime.Now.AddDays(-20) }
            }
        },
        // Nivel 3: Socio castigado de MINERA ALTIPLANO (el problema mas profundo)
        ["30303030"] = new Persona
        {
            Dni = "30303030",
            Nombres = "Hector",
            Apellidos = "Villanueva Cruz",
            ScoreCrediticio = 180,
            Estado = EstadoCrediticio.Castigado,
            Deudas = new List<DeudaRegistrada>
            {
                new() { Entidad = "Scotiabank", TipoDeuda = "Préstamo Comercial", MontoOriginal = 100000, SaldoActual = 150000, DiasVencidos = 300, Calificacion = "Pérdida", FechaVencimiento = DateTime.Now.AddDays(-300) },
                new() { Entidad = "SUNAT", TipoDeuda = "Deuda Tributaria", MontoOriginal = 80000, SaldoActual = 95000, DiasVencidos = 240, Calificacion = "Pérdida", FechaVencimiento = DateTime.Now.AddDays(-240) }
            }
        },
        // Nivel 3: Otro socio de MINERA ALTIPLANO con morosidad
        ["40404040"] = new Persona
        {
            Dni = "40404040",
            Nombres = "Isabel",
            Apellidos = "Chavez Rios",
            ScoreCrediticio = 350,
            Estado = EstadoCrediticio.Moroso,
            Deudas = new List<DeudaRegistrada>
            {
                new() { Entidad = "BCP", TipoDeuda = "Línea de Crédito", MontoOriginal = 40000, SaldoActual = 55000, DiasVencidos = 150, Calificacion = "Dudoso", FechaVencimiento = DateTime.Now.AddDays(-150) }
            }
        }
    };

    // Base de datos simulada de empresas
    private readonly Dictionary<string, Empresa> _empresas = new()
    {
        ["20123456789"] = new Empresa
        {
            Ruc = "20123456789",
            RazonSocial = "DISTRIBUIDORA COMERCIAL DEL NORTE SAC",
            NombreComercial = "DICONOR",
            Estado = "ACTIVO",
            Direccion = "Av. Industrial 1234, Los Olivos, Lima",
            ScoreCrediticio = 720,
            EstadoCredito = EstadoCrediticio.Normal,
            Deudas = new List<DeudaRegistrada>
            {
                new() { Entidad = "BCP", TipoDeuda = "Línea de Crédito", MontoOriginal = 100000, SaldoActual = 45000, DiasVencidos = 0, Calificacion = "Normal" },
                new() { Entidad = "BBVA", TipoDeuda = "Leasing", MontoOriginal = 80000, SaldoActual = 50000, DiasVencidos = 0, Calificacion = "Normal" }
            }
        },
        ["20987654321"] = new Empresa
        {
            Ruc = "20987654321",
            RazonSocial = "TECNOLOGÍA AVANZADA PERÚ EIRL",
            NombreComercial = "TECPERU",
            Estado = "ACTIVO",
            Direccion = "Jr. Tecnología 567, San Isidro, Lima",
            ScoreCrediticio = 650,
            EstadoCredito = EstadoCrediticio.Normal,
            Deudas = new List<DeudaRegistrada>
            {
                new() { Entidad = "Interbank", TipoDeuda = "Capital de Trabajo", MontoOriginal = 200000, SaldoActual = 120000, DiasVencidos = 0, Calificacion = "Normal" }
            }
        },
        ["20111111111"] = new Empresa
        {
            Ruc = "20111111111",
            RazonSocial = "IMPORTACIONES GLOBALES SAC",
            NombreComercial = "IMPOGLOBAL",
            Estado = "ACTIVO",
            Direccion = "Calle Comercio 890, Callao",
            ScoreCrediticio = 380,
            EstadoCredito = EstadoCrediticio.Moroso,
            Deudas = new List<DeudaRegistrada>
            {
                new() { Entidad = "Scotiabank", TipoDeuda = "Línea de Crédito", MontoOriginal = 150000, SaldoActual = 180000, DiasVencidos = 60, Calificacion = "Deficiente", FechaVencimiento = DateTime.Now.AddDays(-60) },
                new() { Entidad = "SUNAT", TipoDeuda = "Deuda Tributaria", MontoOriginal = 45000, SaldoActual = 52000, DiasVencidos = 90, Calificacion = "Dudoso", FechaVencimiento = DateTime.Now.AddDays(-90) }
            }
        },
        ["20222222222"] = new Empresa
        {
            Ruc = "20222222222",
            RazonSocial = "CONSTRUCTORA ANDES SRL",
            NombreComercial = "CONANDES",
            Estado = "BAJA",
            Direccion = "Av. Construcción 111, Surco, Lima",
            ScoreCrediticio = 0,
            EstadoCredito = EstadoCrediticio.Castigado,
            Deudas = new List<DeudaRegistrada>
            {
                new() { Entidad = "BCP", TipoDeuda = "Préstamo Comercial", MontoOriginal = 500000, SaldoActual = 650000, DiasVencidos = 365, Calificacion = "Pérdida", FechaVencimiento = DateTime.Now.AddDays(-365) }
            }
        },
        ["20333333333"] = new Empresa
        {
            Ruc = "20333333333",
            RazonSocial = "SERVICIOS LOGÍSTICOS EXPRESS SAC",
            NombreComercial = "SERVILOG",
            Estado = "ACTIVO",
            Direccion = "Av. Argentina 2000, Cercado de Lima",
            ScoreCrediticio = 690,
            EstadoCredito = EstadoCrediticio.Normal,
            Deudas = new List<DeudaRegistrada>
            {
                new() { Entidad = "BBVA", TipoDeuda = "Factoring", MontoOriginal = 75000, SaldoActual = 25000, DiasVencidos = 0, Calificacion = "Normal" }
            }
        },
        // CASO ESPECIAL: Empresa limpia pero con socios problematicos
        ["20444444444"] = new Empresa
        {
            Ruc = "20444444444",
            RazonSocial = "INVERSIONES ANDINAS SAC",
            NombreComercial = "INVANDINAS",
            Estado = "ACTIVO",
            Direccion = "Av. Javier Prado 3500, San Borja, Lima",
            ScoreCrediticio = 750,
            EstadoCredito = EstadoCrediticio.Normal,
            Deudas = new List<DeudaRegistrada>
            {
                new() { Entidad = "BCP", TipoDeuda = "Línea de Crédito", MontoOriginal = 200000, SaldoActual = 50000, DiasVencidos = 0, Calificacion = "Normal" },
                new() { Entidad = "Interbank", TipoDeuda = "Leasing Vehicular", MontoOriginal = 120000, SaldoActual = 80000, DiasVencidos = 0, Calificacion = "Normal" }
            }
        },

        // === CASO RED PROFUNDA (3 niveles) ===
        // Nivel 0: Empresa principal limpia
        ["20555555555"] = new Empresa
        {
            Ruc = "20555555555",
            RazonSocial = "CONSULTORES ASOCIADOS DEL SUR SAC",
            NombreComercial = "CONSURSUR",
            Estado = "ACTIVO",
            Direccion = "Av. Reducto 1500, Miraflores, Lima",
            ScoreCrediticio = 780,
            EstadoCredito = EstadoCrediticio.Normal,
            Deudas = new List<DeudaRegistrada>
            {
                new() { Entidad = "BCP", TipoDeuda = "Línea de Crédito", MontoOriginal = 300000, SaldoActual = 80000, DiasVencidos = 0, Calificacion = "Normal" }
            }
        },
        // Nivel 1: Empresa donde Fernando (10101010) tambien es socio
        ["20666666666"] = new Empresa
        {
            Ruc = "20666666666",
            RazonSocial = "DISTRIBUCIONES PACIFICO EIRL",
            NombreComercial = "DISPACIF",
            Estado = "ACTIVO",
            Direccion = "Jr. Union 800, Cercado de Lima",
            ScoreCrediticio = 680,
            EstadoCredito = EstadoCrediticio.Normal,
            Deudas = new List<DeudaRegistrada>
            {
                new() { Entidad = "BBVA", TipoDeuda = "Capital de Trabajo", MontoOriginal = 150000, SaldoActual = 90000, DiasVencidos = 0, Calificacion = "Normal" }
            }
        },
        // Nivel 2: Empresa donde Gabriela (20202020) es socia - esta empresa tiene socios problematicos
        ["20777777777"] = new Empresa
        {
            Ruc = "20777777777",
            RazonSocial = "MINERA ALTIPLANO SAC",
            NombreComercial = "MINERALTI",
            Estado = "ACTIVO",
            Direccion = "Av. Ejercito 500, Arequipa",
            ScoreCrediticio = 450,
            EstadoCredito = EstadoCrediticio.ConProblemasPotenciales,
            Deudas = new List<DeudaRegistrada>
            {
                new() { Entidad = "Scotiabank", TipoDeuda = "Préstamo Comercial", MontoOriginal = 500000, SaldoActual = 350000, DiasVencidos = 30, Calificacion = "CPP", FechaVencimiento = DateTime.Now.AddDays(-30) }
            }
        }
    };

    // Relaciones entre personas y empresas
    private readonly List<RelacionSocietaria> _relaciones = new()
    {
        // Juan Carlos Pérez - Socio principal de DICONOR
        new() { Dni = "12345678", NombrePersona = "Juan Carlos Pérez García", Ruc = "20123456789", RazonSocialEmpresa = "DISTRIBUIDORA COMERCIAL DEL NORTE SAC", TipoRelacion = "Gerente General", PorcentajeParticipacion = 60, EsActiva = true },
        new() { Dni = "12345678", NombrePersona = "Juan Carlos Pérez García", Ruc = "20987654321", RazonSocialEmpresa = "TECNOLOGÍA AVANZADA PERÚ EIRL", TipoRelacion = "Accionista", PorcentajeParticipacion = 25, EsActiva = true },

        // María Elena López - Socia de DICONOR y TECPERU
        new() { Dni = "87654321", NombrePersona = "María Elena López Torres", Ruc = "20123456789", RazonSocialEmpresa = "DISTRIBUIDORA COMERCIAL DEL NORTE SAC", TipoRelacion = "Accionista", PorcentajeParticipacion = 40, EsActiva = true },
        new() { Dni = "87654321", NombrePersona = "María Elena López Torres", Ruc = "20987654321", RazonSocialEmpresa = "TECNOLOGÍA AVANZADA PERÚ EIRL", TipoRelacion = "Gerente General", PorcentajeParticipacion = 75, EsActiva = true },

        // Roberto Sánchez (moroso) - Socio de IMPOGLOBAL
        new() { Dni = "11111111", NombrePersona = "Roberto Sánchez Medina", Ruc = "20111111111", RazonSocialEmpresa = "IMPORTACIONES GLOBALES SAC", TipoRelacion = "Gerente General", PorcentajeParticipacion = 80, EsActiva = true },
        new() { Dni = "11111111", NombrePersona = "Roberto Sánchez Medina", Ruc = "20333333333", RazonSocialEmpresa = "SERVICIOS LOGÍSTICOS EXPRESS SAC", TipoRelacion = "Accionista", PorcentajeParticipacion = 15, EsActiva = true },

        // Ana Patricia - Socia de IMPOGLOBAL
        new() { Dni = "22222222", NombrePersona = "Ana Patricia Fernández Ruiz", Ruc = "20111111111", RazonSocialEmpresa = "IMPORTACIONES GLOBALES SAC", TipoRelacion = "Accionista", PorcentajeParticipacion = 20, EsActiva = true },

        // Carlos Alberto (castigado) - Ex socio de CONANDES
        new() { Dni = "33333333", NombrePersona = "Carlos Alberto Mendoza Quispe", Ruc = "20222222222", RazonSocialEmpresa = "CONSTRUCTORA ANDES SRL", TipoRelacion = "Ex Gerente General", PorcentajeParticipacion = 100, EsActiva = false },

        // Luis Fernando - Socio de SERVILOG
        new() { Dni = "44444444", NombrePersona = "Luis Fernando Castro Vega", Ruc = "20333333333", RazonSocialEmpresa = "SERVICIOS LOGÍSTICOS EXPRESS SAC", TipoRelacion = "Gerente General", PorcentajeParticipacion = 70, EsActiva = true },

        // Patricia Rojas - Socia de SERVILOG
        new() { Dni = "55555555", NombrePersona = "Patricia Rojas Díaz", Ruc = "20333333333", RazonSocialEmpresa = "SERVICIOS LOGÍSTICOS EXPRESS SAC", TipoRelacion = "Accionista", PorcentajeParticipacion = 15, EsActiva = true },

        // CASO ESPECIAL: Ricardo Vargas (limpio) es socio de INVANDINAS (limpia)
        // Pero INVANDINAS tiene otros socios problematicos (Eduardo y Carmen)
        new() { Dni = "66666666", NombrePersona = "Ricardo Vargas Mendoza", Ruc = "20444444444", RazonSocialEmpresa = "INVERSIONES ANDINAS SAC", TipoRelacion = "Gerente General", PorcentajeParticipacion = 40, EsActiva = true },

        // Eduardo Quispe (MOROSO) - Socio de INVANDINAS
        new() { Dni = "77777777", NombrePersona = "Eduardo Quispe Mamani", Ruc = "20444444444", RazonSocialEmpresa = "INVERSIONES ANDINAS SAC", TipoRelacion = "Accionista", PorcentajeParticipacion = 35, EsActiva = true },

        // Carmen Flores (CASTIGADA) - Socia de INVANDINAS
        new() { Dni = "88888888", NombrePersona = "Carmen Flores Huanca", Ruc = "20444444444", RazonSocialEmpresa = "INVERSIONES ANDINAS SAC", TipoRelacion = "Accionista", PorcentajeParticipacion = 25, EsActiva = true },

        // === CASO RED PROFUNDA (3 niveles) ===
        // Nivel 0: Alberto Ramirez es Gerente de CONSURSUR
        new() { Dni = "99999999", NombrePersona = "Alberto Ramirez Soto", Ruc = "20555555555", RazonSocialEmpresa = "CONSULTORES ASOCIADOS DEL SUR SAC", TipoRelacion = "Gerente General", PorcentajeParticipacion = 50, EsActiva = true },

        // Nivel 1: Fernando Gutierrez es socio de CONSURSUR (mismo nivel que solicitante)
        new() { Dni = "10101010", NombrePersona = "Fernando Gutierrez Palacios", Ruc = "20555555555", RazonSocialEmpresa = "CONSULTORES ASOCIADOS DEL SUR SAC", TipoRelacion = "Accionista", PorcentajeParticipacion = 50, EsActiva = true },
        // Fernando tambien es Gerente de DISTRIBUCIONES PACIFICO (nivel 1 -> nivel 2)
        new() { Dni = "10101010", NombrePersona = "Fernando Gutierrez Palacios", Ruc = "20666666666", RazonSocialEmpresa = "DISTRIBUCIONES PACIFICO EIRL", TipoRelacion = "Gerente General", PorcentajeParticipacion = 60, EsActiva = true },

        // Nivel 2: Gabriela Torres es socia de DISTRIBUCIONES PACIFICO
        new() { Dni = "20202020", NombrePersona = "Gabriela Torres Medina", Ruc = "20666666666", RazonSocialEmpresa = "DISTRIBUCIONES PACIFICO EIRL", TipoRelacion = "Accionista", PorcentajeParticipacion = 40, EsActiva = true },
        // Gabriela tambien es socia de MINERA ALTIPLANO (nivel 2 -> nivel 3)
        new() { Dni = "20202020", NombrePersona = "Gabriela Torres Medina", Ruc = "20777777777", RazonSocialEmpresa = "MINERA ALTIPLANO SAC", TipoRelacion = "Accionista", PorcentajeParticipacion = 30, EsActiva = true },

        // Nivel 3: Hector Villanueva (CASTIGADO) es Gerente de MINERA ALTIPLANO
        new() { Dni = "30303030", NombrePersona = "Hector Villanueva Cruz", Ruc = "20777777777", RazonSocialEmpresa = "MINERA ALTIPLANO SAC", TipoRelacion = "Gerente General", PorcentajeParticipacion = 45, EsActiva = true },
        // Nivel 3: Isabel Chavez (MOROSA) es socia de MINERA ALTIPLANO
        new() { Dni = "40404040", NombrePersona = "Isabel Chavez Rios", Ruc = "20777777777", RazonSocialEmpresa = "MINERA ALTIPLANO SAC", TipoRelacion = "Accionista", PorcentajeParticipacion = 25, EsActiva = true },
    };

    public Task<Persona?> ConsultarPersonaAsync(string dni, CancellationToken cancellationToken = default)
    {
        // Simular latencia de red
        Task.Delay(100, cancellationToken).Wait(cancellationToken);

        if (_personas.TryGetValue(dni, out var persona))
        {
            persona.FechaConsulta = DateTime.UtcNow;
            return Task.FromResult<Persona?>(persona);
        }

        // Si no existe, crear una persona genérica con score medio
        var personaGenerica = new Persona
        {
            Dni = dni,
            Nombres = "Persona",
            Apellidos = $"No Registrada ({dni})",
            ScoreCrediticio = 550,
            Estado = EstadoCrediticio.SinInformacion,
            Deudas = new List<DeudaRegistrada>(),
            FechaConsulta = DateTime.UtcNow
        };

        return Task.FromResult<Persona?>(personaGenerica);
    }

    public Task<Empresa?> ConsultarEmpresaAsync(string ruc, CancellationToken cancellationToken = default)
    {
        Task.Delay(100, cancellationToken).Wait(cancellationToken);

        if (_empresas.TryGetValue(ruc, out var empresa))
        {
            empresa.FechaConsulta = DateTime.UtcNow;
            return Task.FromResult<Empresa?>(empresa);
        }

        var empresaGenerica = new Empresa
        {
            Ruc = ruc,
            RazonSocial = $"EMPRESA NO REGISTRADA ({ruc})",
            Estado = "NO HABIDO",
            ScoreCrediticio = 400,
            EstadoCredito = EstadoCrediticio.SinInformacion,
            Deudas = new List<DeudaRegistrada>(),
            FechaConsulta = DateTime.UtcNow
        };

        return Task.FromResult<Empresa?>(empresaGenerica);
    }

    public Task<List<RelacionSocietaria>> ObtenerEmpresasDondeEsSocioAsync(string dni, CancellationToken cancellationToken = default)
    {
        Task.Delay(50, cancellationToken).Wait(cancellationToken);

        var relaciones = _relaciones
            .Where(r => r.Dni == dni && r.EsActiva)
            .ToList();

        return Task.FromResult(relaciones);
    }

    public Task<List<RelacionSocietaria>> ObtenerSociosDeEmpresaAsync(string ruc, CancellationToken cancellationToken = default)
    {
        Task.Delay(50, cancellationToken).Wait(cancellationToken);

        var socios = _relaciones
            .Where(r => r.Ruc == ruc && r.EsActiva)
            .ToList();

        return Task.FromResult(socios);
    }
}
