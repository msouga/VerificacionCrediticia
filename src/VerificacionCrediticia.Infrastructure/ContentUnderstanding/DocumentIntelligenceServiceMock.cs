using Microsoft.Extensions.Logging;
using VerificacionCrediticia.Core.DTOs;
using VerificacionCrediticia.Core.Interfaces;

namespace VerificacionCrediticia.Infrastructure.ContentUnderstanding;

/// <summary>
/// Mock de Document Intelligence para desarrollo y pruebas.
/// Retorna datos ficticios de DNI consistentes con los datos de prueba de Equifax mock.
/// Ignora el archivo enviado y retorna datos segun el nombre del archivo o datos genéricos.
/// </summary>
public class DocumentIntelligenceServiceMock : IDocumentIntelligenceService
{
    private readonly ILogger<DocumentIntelligenceServiceMock> _logger;
    private readonly Dictionary<string, DocumentoIdentidadDto> _documentos;

    public DocumentIntelligenceServiceMock(ILogger<DocumentIntelligenceServiceMock> logger)
    {
        _logger = logger;
        _documentos = InicializarDocumentos();
    }

    public async Task<DocumentoIdentidadDto> ProcesarDocumentoIdentidadAsync(
        Stream documentStream,
        string nombreArchivo,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken); // Simular latencia de procesamiento

        _logger.LogInformation("[MOCK] Procesando documento de identidad: {NombreArchivo}", nombreArchivo);

        // Intentar extraer un DNI del nombre del archivo (ej: "dni_12345678.jpg", "12345678.pdf")
        var dniExtraido = ExtraerDniDeNombreArchivo(nombreArchivo);

        if (dniExtraido != null && _documentos.TryGetValue(dniExtraido, out var documento))
        {
            var resultado = ClonarConArchivo(documento, nombreArchivo);
            _logger.LogInformation(
                "[MOCK] Documento encontrado: DNI {Dni}, Nombre: {Nombre} {Apellido}",
                resultado.NumeroDocumento, resultado.Nombres, resultado.Apellidos);
            return resultado;
        }

        // Retornar documento generico
        var generico = CrearDocumentoGenerico(nombreArchivo);
        _logger.LogInformation(
            "[MOCK] Documento generico retornado para: {NombreArchivo}", nombreArchivo);
        return generico;
    }

    public async Task<VigenciaPoderDto> ProcesarVigenciaPoderAsync(
        Stream documentStream,
        string nombreArchivo,
        CancellationToken cancellationToken = default,
        IProgress<string>? progreso = null)
    {
        await Task.Delay(300, cancellationToken);

        _logger.LogInformation("[MOCK] Procesando Vigencia de Poder: {NombreArchivo}", nombreArchivo);

        var nombreLower = nombreArchivo.ToLowerInvariant();
        if (nombreLower.Contains("techsolutions") || nombreLower.Contains("20659018901"))
        {
            return CrearVigenciaTechSolutions(nombreArchivo);
        }

        return CrearVigenciaGenerica(nombreArchivo);
    }

    public async Task<BalanceGeneralDto> ProcesarBalanceGeneralAsync(
        Stream documentStream,
        string nombreArchivo,
        CancellationToken cancellationToken = default,
        IProgress<string>? progreso = null)
    {
        await Task.Delay(400, cancellationToken);

        _logger.LogInformation("[MOCK] Procesando Balance General: {NombreArchivo}", nombreArchivo);

        var nombreLower = nombreArchivo.ToLowerInvariant();
        if (nombreLower.Contains("techsolutions") || nombreLower.Contains("20659018901"))
        {
            return CrearBalanceTechSolutions(nombreArchivo);
        }

        return CrearBalanceGenerico(nombreArchivo);
    }

    private static VigenciaPoderDto CrearVigenciaTechSolutions(string nombreArchivo)
    {
        var confianza = new Dictionary<string, float>
        {
            ["Ruc"] = 0.96f,
            ["RazonSocial"] = 0.95f,
            ["TipoPersonaJuridica"] = 0.90f,
            ["Domicilio"] = 0.88f,
            ["ObjetoSocial"] = 0.85f,
            ["CapitalSocial"] = 0.92f,
            ["PartidaRegistral"] = 0.94f,
            ["FechaConstitucion"] = 0.91f
        };

        return new VigenciaPoderDto
        {
            Ruc = "20659018901",
            RazonSocial = "TECH SOLUTIONS IMPORT SAC",
            TipoPersonaJuridica = "SOCIEDAD ANONIMA CERRADA",
            Domicilio = "AV. JAVIER PRADO ESTE 4600 OFC. 1205, SURCO, LIMA",
            ObjetoSocial = "IMPORTACION Y COMERCIALIZACION DE EQUIPOS DE TECNOLOGIA",
            CapitalSocial = "S/ 500,000.00",
            PartidaRegistral = "14523698",
            FechaConstitucion = "15/03/2018",
            Representantes = new List<RepresentanteDto>
            {
                new()
                {
                    Nombre = "AILEEN MEILYN LEI KOO",
                    DocumentoIdentidad = "46590189",
                    Cargo = "GERENTE GENERAL",
                    FechaNombramiento = "15/03/2018",
                    Facultades = "REPRESENTACION LEGAL, ADMINISTRATIVA Y JUDICIAL"
                },
                new()
                {
                    Nombre = "DIEGO ARMANDO VARGAS RAMOS",
                    DocumentoIdentidad = "51515151",
                    Cargo = "SUB GERENTE",
                    FechaNombramiento = "20/06/2019",
                    Facultades = "REPRESENTACION ADMINISTRATIVA"
                },
                new()
                {
                    Nombre = "LUCIA ANDREA PAREDES SOTO",
                    DocumentoIdentidad = "52525252",
                    Cargo = "APODERADA",
                    FechaNombramiento = "10/01/2020",
                    Facultades = "ACTOS DE COMERCIO Y OPERACIONES BANCARIAS"
                },
                new()
                {
                    Nombre = "RAUL ENRIQUE MONTOYA DIAZ",
                    DocumentoIdentidad = "53535353",
                    Cargo = "APODERADO",
                    FechaNombramiento = "05/08/2021",
                    Facultades = "OPERACIONES BANCARIAS HASTA S/ 100,000.00"
                }
            },
            Confianza = confianza,
            ConfianzaPromedio = confianza.Values.Average(),
            ArchivoOrigen = nombreArchivo
        };
    }

    private static VigenciaPoderDto CrearVigenciaGenerica(string nombreArchivo)
    {
        var confianza = new Dictionary<string, float>
        {
            ["Ruc"] = 0.92f,
            ["RazonSocial"] = 0.90f,
            ["TipoPersonaJuridica"] = 0.88f,
            ["Domicilio"] = 0.85f,
            ["ObjetoSocial"] = 0.82f,
            ["CapitalSocial"] = 0.88f,
            ["PartidaRegistral"] = 0.90f,
            ["FechaConstitucion"] = 0.87f
        };

        return new VigenciaPoderDto
        {
            Ruc = "20123456789",
            RazonSocial = "EMPRESA DE PRUEBA SAC",
            TipoPersonaJuridica = "SOCIEDAD ANONIMA CERRADA",
            Domicilio = "AV. PRUEBA 123, LIMA",
            ObjetoSocial = "ACTIVIDADES EMPRESARIALES DIVERSAS",
            CapitalSocial = "S/ 100,000.00",
            PartidaRegistral = "11111111",
            FechaConstitucion = "01/01/2015",
            Representantes = new List<RepresentanteDto>
            {
                new()
                {
                    Nombre = "JUAN CARLOS PEREZ GARCIA",
                    DocumentoIdentidad = "12345678",
                    Cargo = "GERENTE GENERAL",
                    FechaNombramiento = "01/01/2015",
                    Facultades = "REPRESENTACION LEGAL PLENA"
                },
                new()
                {
                    Nombre = "MARIA ELENA LOPEZ TORRES",
                    DocumentoIdentidad = "87654321",
                    Cargo = "APODERADA",
                    FechaNombramiento = "15/06/2018",
                    Facultades = "ACTOS DE ADMINISTRACION"
                }
            },
            Confianza = confianza,
            ConfianzaPromedio = confianza.Values.Average(),
            ArchivoOrigen = nombreArchivo
        };
    }

    private static string? ExtraerDniDeNombreArchivo(string nombreArchivo)
    {
        // Buscar secuencia de 8 digitos en el nombre del archivo
        var soloNombre = Path.GetFileNameWithoutExtension(nombreArchivo);
        var match = System.Text.RegularExpressions.Regex.Match(soloNombre, @"\d{8}");
        return match.Success ? match.Value : null;
    }

    private static DocumentoIdentidadDto ClonarConArchivo(DocumentoIdentidadDto original, string nombreArchivo)
    {
        return new DocumentoIdentidadDto
        {
            Nombres = original.Nombres,
            Apellidos = original.Apellidos,
            NumeroDocumento = original.NumeroDocumento,
            FechaNacimiento = original.FechaNacimiento,
            FechaExpiracion = original.FechaExpiracion,
            Sexo = original.Sexo,
            EstadoCivil = original.EstadoCivil,
            Direccion = original.Direccion,
            Nacionalidad = original.Nacionalidad,
            TipoDocumento = original.TipoDocumento,
            Confianza = new Dictionary<string, float>(original.Confianza),
            ConfianzaPromedio = original.ConfianzaPromedio,
            ArchivoOrigen = nombreArchivo
        };
    }

    private static DocumentoIdentidadDto CrearDocumentoGenerico(string nombreArchivo)
    {
        var confianza = new Dictionary<string, float>
        {
            ["Nombres"] = 0.92f,
            ["Apellidos"] = 0.90f,
            ["NumeroDocumento"] = 0.95f,
            ["FechaNacimiento"] = 0.88f,
            ["Sexo"] = 0.85f,
            ["EstadoCivil"] = 0.85f
        };

        return new DocumentoIdentidadDto
        {
            Nombres = "DOCUMENTO",
            Apellidos = "DE PRUEBA",
            NumeroDocumento = "00000000",
            FechaNacimiento = "01/01/1990",
            FechaExpiracion = "01/01/2030",
            Sexo = "M",
            EstadoCivil = "S",
            Direccion = "AV. PRUEBA 123, LIMA",
            Nacionalidad = "PER",
            TipoDocumento = "DNI",
            Confianza = confianza,
            ConfianzaPromedio = confianza.Values.Average(),
            ArchivoOrigen = nombreArchivo
        };
    }

    private static Dictionary<string, DocumentoIdentidadDto> InicializarDocumentos()
    {
        var docs = new Dictionary<string, DocumentoIdentidadDto>();

        // Caso 1: Juan Carlos Perez Garcia (red limpia)
        docs["12345678"] = CrearDto(
            nombres: "JUAN CARLOS",
            apellidos: "PEREZ GARCIA",
            dni: "12345678",
            fechaNacimiento: "15/03/1975",
            fechaExpiracion: "15/03/2028",
            sexo: "M",
            estadoCivil: "C",
            direccion: "AV. LOS OLIVOS 456, LIMA",
            nacionalidad: "PER");

        // Maria Elena Lopez Torres
        docs["87654321"] = CrearDto(
            nombres: "MARIA ELENA",
            apellidos: "LOPEZ TORRES",
            dni: "87654321",
            fechaNacimiento: "22/07/1980",
            fechaExpiracion: "22/07/2027",
            sexo: "F",
            estadoCivil: "S",
            direccion: "JR. HUALLAGA 789, LIMA",
            nacionalidad: "PER");

        // Caso 2: Luis Fernando Castro Vega (moroso)
        docs["44444444"] = CrearDto(
            nombres: "LUIS FERNANDO",
            apellidos: "CASTRO VEGA",
            dni: "44444444",
            fechaNacimiento: "10/11/1968",
            fechaExpiracion: "10/11/2026",
            sexo: "M",
            estadoCivil: "D",
            direccion: "CALLE LAS FLORES 321, SAN ISIDRO",
            nacionalidad: "PER");

        // Ana Patricia Fernandez Ruiz
        docs["22222222"] = CrearDto(
            nombres: "ANA PATRICIA",
            apellidos: "FERNANDEZ RUIZ",
            dni: "22222222",
            fechaNacimiento: "28/04/1982",
            fechaExpiracion: "28/04/2029",
            sexo: "F",
            estadoCivil: "S",
            direccion: "AV. JAVIER PRADO 1234, SAN BORJA",
            nacionalidad: "PER");

        // Caso 3: Patricia Rojas Diaz
        docs["55555555"] = CrearDto(
            nombres: "PATRICIA",
            apellidos: "ROJAS DIAZ",
            dni: "55555555",
            fechaNacimiento: "03/09/1977",
            fechaExpiracion: "03/09/2027",
            sexo: "F",
            estadoCivil: "C",
            direccion: "AV. AREQUIPA 567, MIRAFLORES",
            nacionalidad: "PER");

        // Carlos Alberto Mendoza Quispe
        docs["33333333"] = CrearDto(
            nombres: "CARLOS ALBERTO",
            apellidos: "MENDOZA QUISPE",
            dni: "33333333",
            fechaNacimiento: "12/12/1965",
            fechaExpiracion: "12/12/2025",
            sexo: "M",
            estadoCivil: "V",
            direccion: "PASAJE SAN MARTIN 45, CUSCO",
            nacionalidad: "PER");

        // Caso 4: Ricardo Vargas Mendoza (red con socios problematicos)
        docs["66666666"] = CrearDto(
            nombres: "RICARDO",
            apellidos: "VARGAS MENDOZA",
            dni: "66666666",
            fechaNacimiento: "18/05/1972",
            fechaExpiracion: "18/05/2028",
            sexo: "M",
            estadoCivil: "C",
            direccion: "AV. BENAVIDES 890, SURCO",
            nacionalidad: "PER");

        // Eduardo Quispe Mamani
        docs["77777777"] = CrearDto(
            nombres: "EDUARDO",
            apellidos: "QUISPE MAMANI",
            dni: "77777777",
            fechaNacimiento: "25/01/1985",
            fechaExpiracion: "25/01/2029",
            sexo: "M",
            estadoCivil: "S",
            direccion: "JR. PUNO 234, JULIACA",
            nacionalidad: "PER");

        // Carmen Flores Huanca
        docs["88888888"] = CrearDto(
            nombres: "CARMEN",
            apellidos: "FLORES HUANCA",
            dni: "88888888",
            fechaNacimiento: "07/08/1978",
            fechaExpiracion: "07/08/2026",
            sexo: "F",
            estadoCivil: "C",
            direccion: "AV. EL SOL 567, PUNO",
            nacionalidad: "PER");

        // Caso 5: Alberto Ramirez Soto (cadena 3 niveles)
        docs["99999999"] = CrearDto(
            nombres: "ALBERTO",
            apellidos: "RAMIREZ SOTO",
            dni: "99999999",
            fechaNacimiento: "20/02/1970",
            fechaExpiracion: "20/02/2028",
            sexo: "M",
            estadoCivil: "C",
            direccion: "AV. GRAU 1234, AREQUIPA",
            nacionalidad: "PER");

        // Fernando Gutierrez Palacios
        docs["10101010"] = CrearDto(
            nombres: "FERNANDO",
            apellidos: "GUTIERREZ PALACIOS",
            dni: "10101010",
            fechaNacimiento: "11/11/1980",
            fechaExpiracion: "11/11/2028",
            sexo: "M",
            estadoCivil: "C",
            direccion: "CALLE BOLIVAR 456, TRUJILLO",
            nacionalidad: "PER");

        // Gabriela Torres Medina
        docs["20202020"] = CrearDto(
            nombres: "GABRIELA",
            apellidos: "TORRES MEDINA",
            dni: "20202020",
            fechaNacimiento: "14/06/1983",
            fechaExpiracion: "14/06/2029",
            sexo: "F",
            estadoCivil: "S",
            direccion: "AV. LARCO 789, MIRAFLORES",
            nacionalidad: "PER");

        // Hector Villanueva Cruz
        docs["30303030"] = CrearDto(
            nombres: "HECTOR",
            apellidos: "VILLANUEVA CRUZ",
            dni: "30303030",
            fechaNacimiento: "30/10/1960",
            fechaExpiracion: "30/10/2025",
            sexo: "M",
            estadoCivil: "D",
            direccion: "JR. AYACUCHO 123, HUANCAYO",
            nacionalidad: "PER");

        // Isabel Chavez Rios
        docs["40404040"] = CrearDto(
            nombres: "ISABEL",
            apellidos: "CHAVEZ RIOS",
            dni: "40404040",
            fechaNacimiento: "09/04/1988",
            fechaExpiracion: "09/04/2030",
            sexo: "F",
            estadoCivil: "S",
            direccion: "AV. SALAVERRY 345, JESUS MARIA",
            nacionalidad: "PER");

        return docs;
    }

    private static DocumentoIdentidadDto CrearDto(
        string nombres,
        string apellidos,
        string dni,
        string fechaNacimiento,
        string fechaExpiracion,
        string sexo,
        string estadoCivil,
        string direccion,
        string nacionalidad)
    {
        var confianza = new Dictionary<string, float>
        {
            ["Nombres"] = 0.95f,
            ["Apellidos"] = 0.94f,
            ["NumeroDocumento"] = 0.98f,
            ["FechaNacimiento"] = 0.92f,
            ["FechaExpiracion"] = 0.91f,
            ["Sexo"] = 0.85f,
            ["EstadoCivil"] = 0.85f,
            ["Direccion"] = 0.78f,
            ["Nacionalidad"] = 0.96f
        };

        return new DocumentoIdentidadDto
        {
            Nombres = nombres,
            Apellidos = apellidos,
            NumeroDocumento = dni,
            FechaNacimiento = fechaNacimiento,
            FechaExpiracion = fechaExpiracion,
            Sexo = sexo,
            EstadoCivil = estadoCivil,
            Direccion = direccion,
            Nacionalidad = nacionalidad,
            TipoDocumento = "DNI",
            Confianza = confianza,
            ConfianzaPromedio = confianza.Values.Average()
        };
    }

    private static BalanceGeneralDto CrearBalanceTechSolutions(string nombreArchivo)
    {
        var confianza = new Dictionary<string, float>
        {
            ["Ruc"] = 0.98f,
            ["RazonSocial"] = 0.96f,
            ["FechaBalance"] = 0.94f,
            ["TotalActivo"] = 0.92f,
            ["TotalPasivo"] = 0.90f,
            ["TotalPatrimonio"] = 0.91f,
            ["ResultadoEjercicio"] = 0.89f,
            ["EfectivoEquivalentes"] = 0.85f,
            ["CuentasCobrarComerciales"] = 0.88f,
            ["Existencias"] = 0.87f
        };

        var balance = new BalanceGeneralDto
        {
            // Encabezado
            Ruc = "20659018901",
            RazonSocial = "TECH SOLUTIONS IMPORT SAC",
            Domicilio = "AV. JAVIER PRADO ESTE 4600 OFC. 1205, SURCO, LIMA",
            FechaBalance = "31/12/2025",
            Moneda = "SOLES",

            // Activo Corriente
            EfectivoEquivalentes = 485_320.50m,
            CuentasCobrarComerciales = 890_245.75m,
            CuentasCobrarDiversas = 125_680.20m,
            Existencias = 1_245_890.35m,
            GastosPagadosAnticipado = 45_230.80m,
            TotalActivoCorriente = 2_792_367.60m,

            // Activo No Corriente
            InmueblesMaquinariaEquipo = 850_000.00m,
            DepreciacionAcumulada = -285_420.15m,
            Intangibles = 180_000.00m,
            AmortizacionAcumulada = -45_000.00m,
            ActivoDiferido = 74_432.95m,
            TotalActivoNoCorriente = 774_012.80m,

            // Total Activo
            TotalActivo = 3_566_380.40m,

            // Pasivo Corriente
            TributosPorPagar = 185_240.60m,
            RemuneracionesPorPagar = 125_890.45m,
            CuentasPagarComerciales = 650_780.25m,
            ObligacionesFinancierasCorto = 480_000.00m,
            OtrasCuentasPorPagar = 89_320.15m,
            TotalPasivoCorriente = 1_531_231.45m,

            // Pasivo No Corriente
            ObligacionesFinancierasLargo = 1_200_000.00m,
            Provisiones = 89_838.50m,
            TotalPasivoNoCorriente = 1_289_838.50m,

            // Total Pasivo
            TotalPasivo = 2_821_069.95m,

            // Patrimonio
            CapitalSocial = 500_000.00m,
            ReservaLegal = 50_000.00m,
            ResultadosAcumulados = -42_189.55m,
            ResultadoEjercicio = 237_500.00m,
            TotalPatrimonio = 745_310.45m,

            // Total Pasivo + Patrimonio
            TotalPasivoPatrimonio = 3_566_380.40m,

            // Firmantes
            Firmantes = new List<FirmanteDto>
            {
                new()
                {
                    Nombre = "AILEEN MEILYN LEI KOO",
                    Dni = "46590189",
                    Cargo = "GERENTE GENERAL",
                    Matricula = null
                },
                new()
                {
                    Nombre = "MARIA ELENA RODRIGUEZ PAREDES",
                    Dni = "45678912",
                    Cargo = "CONTADOR PUBLICO",
                    Matricula = "CPC 15-4892"
                }
            },

            // Metadata
            Confianza = confianza,
            ConfianzaPromedio = confianza.Values.Average(),
            ArchivoOrigen = nombreArchivo
        };

        // Calcular ratios
        CalcularRatiosFinancieros(balance);

        return balance;
    }

    private static BalanceGeneralDto CrearBalanceGenerico(string nombreArchivo)
    {
        var confianza = new Dictionary<string, float>
        {
            ["Ruc"] = 0.94f,
            ["RazonSocial"] = 0.92f,
            ["FechaBalance"] = 0.90f,
            ["TotalActivo"] = 0.88f,
            ["TotalPasivo"] = 0.86f,
            ["TotalPatrimonio"] = 0.87f,
            ["ResultadoEjercicio"] = 0.85f
        };

        var balance = new BalanceGeneralDto
        {
            // Encabezado
            Ruc = "20123456789",
            RazonSocial = "EMPRESA DE PRUEBA SAC",
            Domicilio = "AV. PRUEBA 123, LIMA",
            FechaBalance = "31/12/2025",
            Moneda = "SOLES",

            // Activo Corriente
            EfectivoEquivalentes = 180_500.00m,
            CuentasCobrarComerciales = 320_400.00m,
            CuentasCobrarDiversas = 45_200.00m,
            Existencias = 450_600.00m,
            GastosPagadosAnticipado = 15_300.00m,
            TotalActivoCorriente = 1_012_000.00m,

            // Activo No Corriente
            InmueblesMaquinariaEquipo = 550_000.00m,
            DepreciacionAcumulada = -180_000.00m,
            Intangibles = 80_000.00m,
            AmortizacionAcumulada = -25_000.00m,
            ActivoDiferido = 35_000.00m,
            TotalActivoNoCorriente = 460_000.00m,

            // Total Activo
            TotalActivo = 1_472_000.00m,

            // Pasivo Corriente
            TributosPorPagar = 85_400.00m,
            RemuneracionesPorPagar = 65_800.00m,
            CuentasPagarComerciales = 280_500.00m,
            ObligacionesFinancierasCorto = 180_000.00m,
            OtrasCuentasPorPagar = 38_300.00m,
            TotalPasivoCorriente = 650_000.00m,

            // Pasivo No Corriente
            ObligacionesFinancierasLargo = 450_000.00m,
            Provisiones = 32_000.00m,
            TotalPasivoNoCorriente = 482_000.00m,

            // Total Pasivo
            TotalPasivo = 1_132_000.00m,

            // Patrimonio
            CapitalSocial = 200_000.00m,
            ReservaLegal = 20_000.00m,
            ResultadosAcumulados = 75_000.00m,
            ResultadoEjercicio = 45_000.00m,
            TotalPatrimonio = 340_000.00m,

            // Total Pasivo + Patrimonio
            TotalPasivoPatrimonio = 1_472_000.00m,

            // Firmantes
            Firmantes = new List<FirmanteDto>
            {
                new()
                {
                    Nombre = "JUAN CARLOS PEREZ GARCIA",
                    Dni = "12345678",
                    Cargo = "GERENTE GENERAL",
                    Matricula = null
                },
                new()
                {
                    Nombre = "LUCIA FERNANDEZ MARTINEZ",
                    Dni = "98765432",
                    Cargo = "CONTADOR PUBLICO",
                    Matricula = "CPC 12-3456"
                }
            },

            // Metadata
            Confianza = confianza,
            ConfianzaPromedio = confianza.Values.Average(),
            ArchivoOrigen = nombreArchivo
        };

        // Calcular ratios
        CalcularRatiosFinancieros(balance);

        return balance;
    }

    private static void CalcularRatiosFinancieros(BalanceGeneralDto balance)
    {
        // Ratio de Liquidez = Activo Corriente / Pasivo Corriente
        if (balance.TotalActivoCorriente > 0 && balance.TotalPasivoCorriente > 0)
        {
            balance.RatioLiquidez = Math.Round(
                balance.TotalActivoCorriente.Value / balance.TotalPasivoCorriente.Value, 2);
        }

        // Ratio de Endeudamiento = Total Pasivo / Total Activo
        if (balance.TotalPasivo > 0 && balance.TotalActivo > 0)
        {
            balance.RatioEndeudamiento = Math.Round(
                balance.TotalPasivo.Value / balance.TotalActivo.Value, 2);
        }

        // Ratio de Solvencia = Total Patrimonio / Total Activo
        if (balance.TotalPatrimonio > 0 && balance.TotalActivo > 0)
        {
            balance.RatioSolvencia = Math.Round(
                balance.TotalPatrimonio.Value / balance.TotalActivo.Value, 2);
        }

        // Capital de Trabajo = Activo Corriente - Pasivo Corriente
        if (balance.TotalActivoCorriente > 0 && balance.TotalPasivoCorriente > 0)
        {
            balance.CapitalTrabajo = Math.Round(
                balance.TotalActivoCorriente.Value - balance.TotalPasivoCorriente.Value, 2);
        }
    }

    public async Task<FichaRucDto> ProcesarFichaRucAsync(
        Stream documentStream,
        string nombreArchivo,
        CancellationToken cancellationToken = default,
        IProgress<string>? progreso = null)
    {
        await Task.Delay(150, cancellationToken);
        progreso?.Report("Analizando Ficha RUC...");
        await Task.Delay(150, cancellationToken);
        progreso?.Report("Extrayendo datos del contribuyente...");

        _logger.LogInformation("[MOCK] Procesando Ficha RUC: {NombreArchivo}", nombreArchivo);

        var nombreLower = nombreArchivo.ToLowerInvariant();
        if (nombreLower.Contains("techsolutions") || nombreLower.Contains("20659018901"))
        {
            return CrearFichaRucTechSolutions(nombreArchivo);
        }

        return CrearFichaRucGenerica(nombreArchivo);
    }

    private static FichaRucDto CrearFichaRucTechSolutions(string nombreArchivo)
    {
        var confianza = new Dictionary<string, float>
        {
            ["Ruc"] = 0.98f,
            ["RazonSocial"] = 0.96f,
            ["NombreComercial"] = 0.90f,
            ["TipoContribuyente"] = 0.94f,
            ["FechaInscripcion"] = 0.92f,
            ["FechaInicioActividades"] = 0.91f,
            ["EstadoContribuyente"] = 0.97f,
            ["CondicionDomicilio"] = 0.96f,
            ["DomicilioFiscal"] = 0.88f,
            ["ActividadEconomica"] = 0.93f,
            ["SistemaContabilidad"] = 0.89f,
            ["ComprobantesAutorizados"] = 0.87f
        };

        return new FichaRucDto
        {
            Ruc = "20659018901",
            RazonSocial = "TECH SOLUTIONS IMPORT SAC",
            NombreComercial = "TECH SOLUTIONS",
            TipoContribuyente = "SOCIEDAD ANONIMA CERRADA",
            FechaInscripcion = "20/03/2018",
            FechaInicioActividades = "01/04/2018",
            EstadoContribuyente = "ACTIVO",
            CondicionDomicilio = "HABIDO",
            DomicilioFiscal = "AV. JAVIER PRADO ESTE 4600 OFC. 1205, SANTIAGO DE SURCO, LIMA, LIMA",
            ActividadEconomica = "4651 - VENTA AL POR MAYOR DE COMPUTADORAS Y EQUIPO PERIFERICO",
            SistemaContabilidad = "COMPUTARIZADO",
            ComprobantesAutorizados = "FACTURA, BOLETA DE VENTA, NOTA DE CREDITO, NOTA DE DEBITO, GUIA DE REMISION",
            Confianza = confianza,
            ConfianzaPromedio = confianza.Values.Average(),
            ArchivoOrigen = nombreArchivo
        };
    }

    private static FichaRucDto CrearFichaRucGenerica(string nombreArchivo)
    {
        var confianza = new Dictionary<string, float>
        {
            ["Ruc"] = 0.94f,
            ["RazonSocial"] = 0.92f,
            ["NombreComercial"] = 0.88f,
            ["TipoContribuyente"] = 0.90f,
            ["FechaInscripcion"] = 0.88f,
            ["FechaInicioActividades"] = 0.87f,
            ["EstadoContribuyente"] = 0.93f,
            ["CondicionDomicilio"] = 0.92f,
            ["DomicilioFiscal"] = 0.85f,
            ["ActividadEconomica"] = 0.89f,
            ["SistemaContabilidad"] = 0.86f,
            ["ComprobantesAutorizados"] = 0.84f
        };

        return new FichaRucDto
        {
            Ruc = "20123456789",
            RazonSocial = "EMPRESA DE PRUEBA SAC",
            NombreComercial = "EMPRESA PRUEBA",
            TipoContribuyente = "SOCIEDAD ANONIMA CERRADA",
            FechaInscripcion = "15/01/2015",
            FechaInicioActividades = "01/02/2015",
            EstadoContribuyente = "ACTIVO",
            CondicionDomicilio = "HABIDO",
            DomicilioFiscal = "AV. PRUEBA 123, LIMA, LIMA, LIMA",
            ActividadEconomica = "4690 - VENTA AL POR MAYOR NO ESPECIALIZADA",
            SistemaContabilidad = "MANUAL",
            ComprobantesAutorizados = "FACTURA, BOLETA DE VENTA",
            Confianza = confianza,
            ConfianzaPromedio = confianza.Values.Average(),
            ArchivoOrigen = nombreArchivo
        };
    }

    public async Task<ClasificacionResultadoDto> ClasificarYProcesarAsync(
        Stream documentStream,
        string nombreArchivo,
        CancellationToken cancellationToken = default,
        IProgress<string>? progreso = null)
    {
        await Task.Delay(300, cancellationToken);
        progreso?.Report("Clasificando documento...");

        _logger.LogInformation("[MOCK] Clasificando documento: {NombreArchivo}", nombreArchivo);

        var nombreLower = nombreArchivo.ToLowerInvariant();

        // Detectar categoria por nombre de archivo
        string categoria;
        if (nombreLower.Contains("dni") || nombreLower.Contains("identidad"))
            categoria = "DNI";
        else if (nombreLower.Contains("vigencia") || nombreLower.Contains("poder"))
            categoria = "VIGENCIA_PODER";
        else if (nombreLower.Contains("balance"))
            categoria = "BALANCE_GENERAL";
        else if (nombreLower.Contains("estado") || nombreLower.Contains("resultado") || nombreLower.Contains("ganancia") || nombreLower.Contains("perdida"))
            categoria = "ESTADO_RESULTADOS";
        else if (nombreLower.Contains("ruc") || nombreLower.Contains("ficha") || nombreLower.Contains("sunat"))
            categoria = "FICHA_RUC";
        else
            categoria = "other";

        _logger.LogInformation("[MOCK] Documento clasificado como: {Categoria}", categoria);

        // Resetear stream para que los metodos de procesamiento puedan leerlo
        if (documentStream.CanSeek)
            documentStream.Position = 0;

        // Procesar segun la categoria detectada
        object? resultado = categoria switch
        {
            "DNI" => await ProcesarDocumentoIdentidadAsync(documentStream, nombreArchivo, cancellationToken),
            "VIGENCIA_PODER" => await ProcesarVigenciaPoderAsync(documentStream, nombreArchivo, cancellationToken, progreso),
            "BALANCE_GENERAL" => await ProcesarBalanceGeneralAsync(documentStream, nombreArchivo, cancellationToken, progreso),
            "ESTADO_RESULTADOS" => await ProcesarEstadoResultadosAsync(documentStream, nombreArchivo, cancellationToken, progreso),
            "FICHA_RUC" => await ProcesarFichaRucAsync(documentStream, nombreArchivo, cancellationToken, progreso),
            _ => null
        };

        return new ClasificacionResultadoDto
        {
            CategoriaDetectada = categoria,
            ResultadoExtraccion = resultado,
            ConfianzaClasificacion = categoria == "other" ? 0.5m : 0.92m
        };
    }

    public async Task<EstadoResultadosDto> ProcesarEstadoResultadosAsync(
        Stream documentStream,
        string nombreArchivo,
        CancellationToken cancellationToken = default,
        IProgress<string>? progreso = null)
    {
        var delay = 2000; // Simular procesamiento
        var pasos = new[]
        {
            "Analizando documento...",
            "Extrayendo partidas del Estado de Resultados...",
            "Calculando ratios financieros...",
            "Validando datos extraídos..."
        };

        for (int i = 0; i < pasos.Length; i++)
        {
            progreso?.Report(pasos[i]);
            await Task.Delay(delay / pasos.Length, cancellationToken);
        }

        _logger.LogInformation("[MOCK] Procesando Estado de Resultados: {NombreArchivo}", nombreArchivo);

        var estadoResultados = CrearEstadoResultadosEjemplo(nombreArchivo);

        _logger.LogInformation(
            "[MOCK] Estado de Resultados procesado: RUC {Ruc}, Ventas: {Ventas:C}, Utilidad Neta: {UtilidadNeta:C}",
            estadoResultados.Ruc, estadoResultados.VentasNetas, estadoResultados.UtilidadNeta);

        return estadoResultados;
    }

    private EstadoResultadosDto CrearEstadoResultadosEjemplo(string nombreArchivo)
    {
        // Si el archivo contiene "TechSolutions" o el RUC de TechSolutions, usar datos específicos
        var esTechSolutions = nombreArchivo.Contains("TechSolutions", StringComparison.OrdinalIgnoreCase) ||
                             nombreArchivo.Contains("20659018901");

        if (esTechSolutions)
        {
            var estado = new EstadoResultadosDto
            {
                Ruc = "20659018901",
                RazonSocial = "TECH SOLUTIONS IMPORT SAC",
                Periodo = "2023",
                Moneda = "Soles",
                VentasNetas = 5200000m,  // 5.2M en ventas
                CostoVentas = 3120000m,  // 60% de costo de ventas
                UtilidadBruta = 2080000m, // 40% margen bruto
                GastosAdministrativos = 520000m, // 10% gastos admin
                GastosVentas = 416000m,  // 8% gastos ventas
                UtilidadOperativa = 1144000m, // ~22% margen operativo
                OtrosIngresos = 52000m,
                OtrosGastos = 26000m,
                UtilidadAntesImpuestos = 1170000m,
                ImpuestoRenta = 249750m, // ~21.4% tasa efectiva
                UtilidadNeta = 920250m,  // ~17.7% margen neto
                ConfianzaPromedio = 0.87m,
                DatosValidosRuc = true,
                FechaProcesado = DateTime.UtcNow
            };

            estado.CalcularRatios();
            return estado;
        }

        // Caso genérico
        var estadoGenerico = new EstadoResultadosDto
        {
            Ruc = "20123456789",
            RazonSocial = "EMPRESA EJEMPLO SAC",
            Periodo = "2023",
            Moneda = "Soles",
            VentasNetas = 1500000m,
            CostoVentas = 900000m,
            UtilidadBruta = 600000m,
            GastosAdministrativos = 180000m,
            GastosVentas = 120000m,
            UtilidadOperativa = 300000m,
            OtrosIngresos = 15000m,
            OtrosGastos = 10000m,
            UtilidadAntesImpuestos = 305000m,
            ImpuestoRenta = 64050m,
            UtilidadNeta = 240950m,
            ConfianzaPromedio = 0.82m,
            DatosValidosRuc = true,
            FechaProcesado = DateTime.UtcNow
        };

        estadoGenerico.CalcularRatios();
        return estadoGenerico;
    }
}
