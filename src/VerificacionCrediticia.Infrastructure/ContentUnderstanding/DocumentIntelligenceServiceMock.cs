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
        CancellationToken cancellationToken = default)
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
}
