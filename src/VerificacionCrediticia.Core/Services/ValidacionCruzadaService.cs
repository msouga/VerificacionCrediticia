using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using VerificacionCrediticia.Core.DTOs;
using VerificacionCrediticia.Core.Entities;
using VerificacionCrediticia.Core.Enums;
using VerificacionCrediticia.Core.Interfaces;

namespace VerificacionCrediticia.Core.Services;

public partial class ValidacionCruzadaService : IValidacionCruzadaService
{
    private readonly ILogger<ValidacionCruzadaService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly string[] PalabrasClaveCredito =
    [
        "credito", "linea de credito", "endeudamiento", "financiamiento",
        "celebrar contratos", "obligaciones", "representacion general",
        "poder general", "amplias facultades", "actos de administracion",
        "actos de disposicion", "contratar", "abrir cuentas"
    ];

    private static readonly string[] SufijosEmpresa =
    [
        "S.A.C.", "SAC", "S.A.", "SA", "S.R.L.", "SRL",
        "E.I.R.L.", "EIRL", "S.C.R.L.", "SCRL", "S.A.A.", "SAA"
    ];

    public ValidacionCruzadaService(ILogger<ValidacionCruzadaService> logger)
    {
        _logger = logger;
    }

    public List<ResultadoValidacionCruzada> ValidarDocumentos(
        Expediente expediente, List<DocumentoProcesado> documentos)
    {
        var resultados = new List<ResultadoValidacionCruzada>();

        // Deserializar datos de cada documento procesado
        var dni = DeserializarDocumento<DocumentoIdentidadDto>(documentos, "DNI");
        var vigencia = DeserializarDocumento<VigenciaPoderDto>(documentos, "VIGENCIA_PODER");
        var balance = DeserializarDocumento<BalanceGeneralDto>(documentos, "BALANCE_GENERAL");
        var estadoResultados = DeserializarDocumento<EstadoResultadosDto>(documentos, "ESTADO_RESULTADOS");
        var fichaRuc = DeserializarDocumento<FichaRucDto>(documentos, "FICHA_RUC");

        // 1. DNI en Vigencia de Poder
        resultados.Add(ValidarDniEnVigenciaPoder(dni, vigencia, expediente));

        // 2. Facultades de credito
        resultados.Add(ValidarFacultadesCredito(dni, vigencia, expediente));

        // 3. RUC consistente
        resultados.Add(ValidarRucConsistente(vigencia, balance, estadoResultados, fichaRuc, expediente));

        // 4. Razon Social consistente
        resultados.Add(ValidarRazonSocialConsistente(vigencia, balance, estadoResultados, fichaRuc));

        // 5. DNI en firmantes del Balance
        resultados.Add(ValidarDniEnFirmantesBalance(dni, vigencia, balance, expediente));

        return resultados;
    }

    private ResultadoValidacionCruzada ValidarDniEnVigenciaPoder(
        DocumentoIdentidadDto? dni, VigenciaPoderDto? vigencia, Expediente expediente)
    {
        var dniNumero = dni?.NumeroDocumento ?? expediente.DniSolicitante;

        if (string.IsNullOrEmpty(dniNumero))
        {
            return new ResultadoValidacionCruzada
            {
                Nombre = "DNI en Vigencia de Poder",
                Aprobada = false,
                Severidad = ResultadoRegla.Rechazar,
                Mensaje = "No se encontro el DNI del solicitante para validar",
                DocumentosInvolucrados = ["DNI", "Vigencia de Poder"]
            };
        }

        if (vigencia == null)
        {
            return new ResultadoValidacionCruzada
            {
                Nombre = "DNI en Vigencia de Poder",
                Aprobada = false,
                Severidad = ResultadoRegla.Rechazar,
                Mensaje = "No se encontro la Vigencia de Poder para validar",
                DocumentosInvolucrados = ["DNI", "Vigencia de Poder"]
            };
        }

        var representanteConDni = vigencia.Representantes
            .FirstOrDefault(r => NormalizarDocumento(r.DocumentoIdentidad) == NormalizarDocumento(dniNumero));

        if (representanteConDni != null)
        {
            return new ResultadoValidacionCruzada
            {
                Nombre = "DNI en Vigencia de Poder",
                Aprobada = true,
                Severidad = ResultadoRegla.Aprobar,
                Mensaje = $"El solicitante (DNI {dniNumero}) aparece como representante: {representanteConDni.Nombre}, cargo: {representanteConDni.Cargo}",
                DocumentosInvolucrados = ["DNI", "Vigencia de Poder"]
            };
        }

        return new ResultadoValidacionCruzada
        {
            Nombre = "DNI en Vigencia de Poder",
            Aprobada = false,
            Severidad = ResultadoRegla.Rechazar,
            Mensaje = $"El solicitante (DNI {dniNumero}) no aparece como representante en la Vigencia de Poder. Representantes encontrados: {string.Join(", ", vigencia.Representantes.Select(r => $"{r.Nombre} (DNI {r.DocumentoIdentidad})"))}",
            DocumentosInvolucrados = ["DNI", "Vigencia de Poder"]
        };
    }

    private ResultadoValidacionCruzada ValidarFacultadesCredito(
        DocumentoIdentidadDto? dni, VigenciaPoderDto? vigencia, Expediente expediente)
    {
        var dniNumero = dni?.NumeroDocumento ?? expediente.DniSolicitante;

        if (string.IsNullOrEmpty(dniNumero) || vigencia == null)
        {
            return new ResultadoValidacionCruzada
            {
                Nombre = "Facultades para credito",
                Aprobada = false,
                Severidad = ResultadoRegla.Revisar,
                Mensaje = "No se pudo verificar facultades: falta DNI o Vigencia de Poder",
                DocumentosInvolucrados = ["DNI", "Vigencia de Poder"]
            };
        }

        var representante = vigencia.Representantes
            .FirstOrDefault(r => NormalizarDocumento(r.DocumentoIdentidad) == NormalizarDocumento(dniNumero));

        if (representante == null)
        {
            return new ResultadoValidacionCruzada
            {
                Nombre = "Facultades para credito",
                Aprobada = false,
                Severidad = ResultadoRegla.Rechazar,
                Mensaje = "El solicitante no es representante; no se pueden verificar facultades",
                DocumentosInvolucrados = ["DNI", "Vigencia de Poder"]
            };
        }

        var facultades = representante.Facultades ?? "";
        var facultadesLower = facultades.ToLowerInvariant();

        var palabrasEncontradas = PalabrasClaveCredito
            .Where(p => facultadesLower.Contains(p))
            .ToList();

        if (palabrasEncontradas.Count > 0)
        {
            return new ResultadoValidacionCruzada
            {
                Nombre = "Facultades para credito",
                Aprobada = true,
                Severidad = ResultadoRegla.Aprobar,
                Mensaje = $"El representante {representante.Nombre} tiene facultades relevantes: {string.Join(", ", palabrasEncontradas)}",
                DocumentosInvolucrados = ["Vigencia de Poder"]
            };
        }

        // Tiene DNI pero no facultades claras: advertencia, no rechazo
        return new ResultadoValidacionCruzada
        {
            Nombre = "Facultades para credito",
            Aprobada = false,
            Severidad = ResultadoRegla.Revisar,
            Mensaje = $"El representante {representante.Nombre} no tiene facultades explicitas para credito. Facultades registradas: {(string.IsNullOrWhiteSpace(facultades) ? "(sin informacion)" : facultades)}",
            DocumentosInvolucrados = ["Vigencia de Poder"]
        };
    }

    private ResultadoValidacionCruzada ValidarRucConsistente(
        VigenciaPoderDto? vigencia, BalanceGeneralDto? balance,
        EstadoResultadosDto? estadoResultados, FichaRucDto? fichaRuc,
        Expediente expediente)
    {
        var rucsEncontrados = new Dictionary<string, string>();

        if (!string.IsNullOrEmpty(expediente.RucEmpresa))
            rucsEncontrados["Expediente"] = NormalizarDocumento(expediente.RucEmpresa)!;
        if (!string.IsNullOrEmpty(vigencia?.Ruc))
            rucsEncontrados["Vigencia de Poder"] = NormalizarDocumento(vigencia.Ruc)!;
        if (!string.IsNullOrEmpty(balance?.Ruc))
            rucsEncontrados["Balance General"] = NormalizarDocumento(balance.Ruc)!;
        if (!string.IsNullOrEmpty(estadoResultados?.Ruc))
            rucsEncontrados["Estado de Resultados"] = NormalizarDocumento(estadoResultados.Ruc)!;
        if (!string.IsNullOrEmpty(fichaRuc?.Ruc))
            rucsEncontrados["Ficha RUC"] = NormalizarDocumento(fichaRuc.Ruc)!;

        if (rucsEncontrados.Count < 2)
        {
            return new ResultadoValidacionCruzada
            {
                Nombre = "RUC consistente",
                Aprobada = true,
                Severidad = ResultadoRegla.Aprobar,
                Mensaje = "No hay suficientes documentos con RUC para comparar",
                DocumentosInvolucrados = rucsEncontrados.Keys.ToList()
            };
        }

        var rucsUnicos = rucsEncontrados.Values.Distinct().ToList();

        if (rucsUnicos.Count == 1)
        {
            return new ResultadoValidacionCruzada
            {
                Nombre = "RUC consistente",
                Aprobada = true,
                Severidad = ResultadoRegla.Aprobar,
                Mensaje = $"Todos los documentos tienen el mismo RUC: {rucsUnicos[0]}",
                DocumentosInvolucrados = rucsEncontrados.Keys.ToList()
            };
        }

        // Hay RUCs diferentes
        var detalle = string.Join(", ", rucsEncontrados.Select(kv => $"{kv.Key}: {kv.Value}"));
        return new ResultadoValidacionCruzada
        {
            Nombre = "RUC consistente",
            Aprobada = false,
            Severidad = ResultadoRegla.Revisar,
            Mensaje = $"Se encontraron RUCs diferentes entre documentos: {detalle}",
            DocumentosInvolucrados = rucsEncontrados.Keys.ToList()
        };
    }

    private ResultadoValidacionCruzada ValidarRazonSocialConsistente(
        VigenciaPoderDto? vigencia, BalanceGeneralDto? balance,
        EstadoResultadosDto? estadoResultados, FichaRucDto? fichaRuc)
    {
        var razonesSociales = new Dictionary<string, string>();

        if (!string.IsNullOrEmpty(vigencia?.RazonSocial))
            razonesSociales["Vigencia de Poder"] = vigencia.RazonSocial;
        if (!string.IsNullOrEmpty(balance?.RazonSocial))
            razonesSociales["Balance General"] = balance.RazonSocial;
        if (!string.IsNullOrEmpty(estadoResultados?.RazonSocial))
            razonesSociales["Estado de Resultados"] = estadoResultados.RazonSocial;
        if (!string.IsNullOrEmpty(fichaRuc?.RazonSocial))
            razonesSociales["Ficha RUC"] = fichaRuc.RazonSocial;

        if (razonesSociales.Count < 2)
        {
            return new ResultadoValidacionCruzada
            {
                Nombre = "Razon Social consistente",
                Aprobada = true,
                Severidad = ResultadoRegla.Aprobar,
                Mensaje = "No hay suficientes documentos con Razon Social para comparar",
                DocumentosInvolucrados = razonesSociales.Keys.ToList()
            };
        }

        // Normalizar y comparar
        var normalizadas = razonesSociales
            .ToDictionary(kv => kv.Key, kv => NormalizarRazonSocial(kv.Value));

        var primeraRazon = normalizadas.Values.First();
        var todasCoinciden = normalizadas.Values.All(rs => SonSimilares(primeraRazon, rs));

        if (todasCoinciden)
        {
            return new ResultadoValidacionCruzada
            {
                Nombre = "Razon Social consistente",
                Aprobada = true,
                Severidad = ResultadoRegla.Aprobar,
                Mensaje = $"La Razon Social es consistente en todos los documentos: {razonesSociales.Values.First()}",
                DocumentosInvolucrados = razonesSociales.Keys.ToList()
            };
        }

        var detalle = string.Join(", ", razonesSociales.Select(kv => $"{kv.Key}: \"{kv.Value}\""));
        return new ResultadoValidacionCruzada
        {
            Nombre = "Razon Social consistente",
            Aprobada = false,
            Severidad = ResultadoRegla.Revisar,
            Mensaje = $"La Razon Social difiere entre documentos: {detalle}",
            DocumentosInvolucrados = razonesSociales.Keys.ToList()
        };
    }

    private ResultadoValidacionCruzada ValidarDniEnFirmantesBalance(
        DocumentoIdentidadDto? dni, VigenciaPoderDto? vigencia,
        BalanceGeneralDto? balance, Expediente expediente)
    {
        if (balance == null || balance.Firmantes.Count == 0)
        {
            return new ResultadoValidacionCruzada
            {
                Nombre = "Firmante del Balance General",
                Aprobada = true,
                Severidad = ResultadoRegla.Aprobar,
                Mensaje = "No se encontraron firmantes en el Balance General para validar",
                DocumentosInvolucrados = ["Balance General"]
            };
        }

        // Recolectar DNIs validos: solicitante + representantes de la vigencia
        var dnisValidos = new HashSet<string>();
        var dniSolicitante = dni?.NumeroDocumento ?? expediente.DniSolicitante;
        if (!string.IsNullOrEmpty(dniSolicitante))
            dnisValidos.Add(NormalizarDocumento(dniSolicitante)!);

        if (vigencia != null)
        {
            foreach (var rep in vigencia.Representantes)
            {
                var docNorm = NormalizarDocumento(rep.DocumentoIdentidad);
                if (!string.IsNullOrEmpty(docNorm))
                    dnisValidos.Add(docNorm);
            }
        }

        if (dnisValidos.Count == 0)
        {
            return new ResultadoValidacionCruzada
            {
                Nombre = "Firmante del Balance General",
                Aprobada = false,
                Severidad = ResultadoRegla.Revisar,
                Mensaje = "No se encontraron DNIs de referencia para validar firmantes",
                DocumentosInvolucrados = ["DNI", "Vigencia de Poder", "Balance General"]
            };
        }

        var firmantesBalance = balance.Firmantes
            .Select(f => new { f.Nombre, Dni = NormalizarDocumento(f.Dni), f.Cargo })
            .ToList();

        var firmantesConocidos = firmantesBalance
            .Where(f => !string.IsNullOrEmpty(f.Dni) && dnisValidos.Contains(f.Dni))
            .ToList();

        if (firmantesConocidos.Count > 0)
        {
            var nombres = string.Join(", ", firmantesConocidos.Select(f => $"{f.Nombre} ({f.Cargo})"));
            return new ResultadoValidacionCruzada
            {
                Nombre = "Firmante del Balance General",
                Aprobada = true,
                Severidad = ResultadoRegla.Aprobar,
                Mensaje = $"Firmante(s) del Balance coinciden con representantes: {nombres}",
                DocumentosInvolucrados = ["DNI", "Vigencia de Poder", "Balance General"]
            };
        }

        var firmantesList = string.Join(", ", firmantesBalance.Select(f => $"{f.Nombre} (DNI {f.Dni ?? "no disponible"})"));
        return new ResultadoValidacionCruzada
        {
            Nombre = "Firmante del Balance General",
            Aprobada = false,
            Severidad = ResultadoRegla.Revisar,
            Mensaje = $"Ninguno de los firmantes del Balance coincide con el solicitante o representantes. Firmantes: {firmantesList}",
            DocumentosInvolucrados = ["DNI", "Vigencia de Poder", "Balance General"]
        };
    }

    // --- Utilidades ---

    private T? DeserializarDocumento<T>(List<DocumentoProcesado> documentos, string codigoTipo) where T : class
    {
        var doc = documentos.FirstOrDefault(d =>
            d.Estado == EstadoDocumento.Procesado &&
            d.TipoDocumento?.Codigo == codigoTipo &&
            !string.IsNullOrEmpty(d.DatosExtraidosJson));

        if (doc == null) return null;

        try
        {
            return JsonSerializer.Deserialize<T>(doc.DatosExtraidosJson!, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "No se pudo deserializar datos del documento {DocId} ({Tipo})",
                doc.Id, codigoTipo);
            return null;
        }
    }

    private static string? NormalizarDocumento(string? doc)
    {
        if (string.IsNullOrWhiteSpace(doc)) return null;
        return SoloDigitos().Replace(doc, "").TrimStart('0');
    }

    private static string NormalizarRazonSocial(string razonSocial)
    {
        var normalizada = razonSocial.ToUpperInvariant().Trim();

        // Remover sufijos empresariales
        foreach (var sufijo in SufijosEmpresa)
        {
            var sufijoUpper = sufijo.ToUpperInvariant();
            if (normalizada.EndsWith(sufijoUpper))
            {
                normalizada = normalizada[..^sufijoUpper.Length].Trim();
            }
        }

        // Remover puntos, comas, guiones
        normalizada = Regex.Replace(normalizada, @"[.,\-]", "");

        // Normalizar espacios multiples
        normalizada = Regex.Replace(normalizada, @"\s+", " ").Trim();

        return normalizada;
    }

    private static bool SonSimilares(string a, string b)
    {
        if (a == b) return true;

        // Similitud basada en Levenshtein
        var maxLen = Math.Max(a.Length, b.Length);
        if (maxLen == 0) return true;

        var distancia = CalcularLevenshtein(a, b);
        var similitud = 1.0 - ((double)distancia / maxLen);

        return similitud >= 0.80;
    }

    private static int CalcularLevenshtein(string a, string b)
    {
        var n = a.Length;
        var m = b.Length;
        var d = new int[n + 1, m + 1];

        for (var i = 0; i <= n; i++) d[i, 0] = i;
        for (var j = 0; j <= m; j++) d[0, j] = j;

        for (var i = 1; i <= n; i++)
        {
            for (var j = 1; j <= m; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }

    [GeneratedRegex(@"\D")]
    private static partial Regex SoloDigitos();
}
