using VerificacionCrediticia.Core.DTOs;
using VerificacionCrediticia.Core.Entities;
using VerificacionCrediticia.Core.Interfaces;

namespace VerificacionCrediticia.Core.Services;

public class DocumentProcessingService : IDocumentProcessingService
{
    private readonly IDocumentIntelligenceService _documentIntelligence;
    private readonly IExpedienteRepository _expedienteRepo;

    public DocumentProcessingService(
        IDocumentIntelligenceService documentIntelligence,
        IExpedienteRepository expedienteRepo)
    {
        _documentIntelligence = documentIntelligence;
        _expedienteRepo = expedienteRepo;
    }

    private static readonly Dictionary<string, string> NombresTipo = new()
    {
        ["DNI"] = "DNI (Documento de Identidad)",
        ["VIGENCIA_PODER"] = "Vigencia de Poder",
        ["BALANCE_GENERAL"] = "Balance General",
        ["ESTADO_RESULTADOS"] = "Estado de Resultados",
        ["FICHA_RUC"] = "Ficha RUC"
    };

    public async Task<object> ProcesarSegunTipoAsync(
        string codigoTipo, Stream documentStream, string fileName,
        CancellationToken cancellationToken = default,
        IProgress<string>? progreso = null)
    {
        // Validar que el codigoTipo sea conocido
        if (!NombresTipo.ContainsKey(codigoTipo))
            throw new NotSupportedException($"Tipo de documento '{codigoTipo}' no tiene procesamiento implementado");

        // Clasificar y extraer en una sola llamada
        var clasificacion = await _documentIntelligence.ClasificarYProcesarAsync(
            documentStream, fileName, cancellationToken, progreso);

        var categoriaDetectada = clasificacion.CategoriaDetectada;

        // Validar que el documento corresponda al slot esperado
        if (categoriaDetectada == "other")
        {
            throw new InvalidOperationException(
                "El documento no corresponde a ningun tipo conocido. Suba un documento valido.");
        }

        if (!string.Equals(categoriaDetectada, codigoTipo, StringComparison.OrdinalIgnoreCase))
        {
            var nombreDetectado = NombresTipo.GetValueOrDefault(categoriaDetectada, categoriaDetectada);
            var nombreEsperado = NombresTipo.GetValueOrDefault(codigoTipo, codigoTipo);
            throw new InvalidOperationException(
                $"El documento parece ser un {nombreDetectado}, no un {nombreEsperado}. Verifique que subio el documento correcto.");
        }

        if (clasificacion.ResultadoExtraccion == null)
        {
            throw new InvalidOperationException(
                $"El clasificador detecto el tipo correcto ({categoriaDetectada}) pero no pudo extraer campos del documento.");
        }

        return clasificacion.ResultadoExtraccion;
    }

    public async Task ActualizarDatosExpedienteAsync(Expediente expediente, string codigoTipo, object resultado)
    {
        bool actualizado = false;

        if (codigoTipo == "DNI" && resultado is DocumentoIdentidadDto dni)
        {
            if (string.IsNullOrEmpty(expediente.DniSolicitante) && !string.IsNullOrEmpty(dni.NumeroDocumento))
            {
                expediente.DniSolicitante = dni.NumeroDocumento;
                actualizado = true;
            }
            if (string.IsNullOrEmpty(expediente.NombresSolicitante) && !string.IsNullOrEmpty(dni.Nombres))
            {
                expediente.NombresSolicitante = dni.Nombres;
                actualizado = true;
            }
            if (string.IsNullOrEmpty(expediente.ApellidosSolicitante) && !string.IsNullOrEmpty(dni.Apellidos))
            {
                expediente.ApellidosSolicitante = dni.Apellidos;
                actualizado = true;
            }
        }

        if (codigoTipo == "VIGENCIA_PODER" && resultado is VigenciaPoderDto vigencia)
        {
            if (string.IsNullOrEmpty(expediente.RazonSocialEmpresa) && !string.IsNullOrEmpty(vigencia.RazonSocial))
            {
                expediente.RazonSocialEmpresa = vigencia.RazonSocial;
                actualizado = true;
            }
            if (string.IsNullOrEmpty(expediente.RucEmpresa) && !string.IsNullOrEmpty(vigencia.Ruc))
            {
                expediente.RucEmpresa = vigencia.Ruc;
                actualizado = true;
            }
        }

        if (codigoTipo == "BALANCE_GENERAL" && resultado is BalanceGeneralDto balance)
        {
            if (string.IsNullOrEmpty(expediente.RazonSocialEmpresa) && !string.IsNullOrEmpty(balance.RazonSocial))
            {
                expediente.RazonSocialEmpresa = balance.RazonSocial;
                actualizado = true;
            }
        }

        if (codigoTipo == "ESTADO_RESULTADOS" && resultado is EstadoResultadosDto estadoRes)
        {
            if (string.IsNullOrEmpty(expediente.RazonSocialEmpresa) && !string.IsNullOrEmpty(estadoRes.RazonSocial))
            {
                expediente.RazonSocialEmpresa = estadoRes.RazonSocial;
                actualizado = true;
            }
        }

        if (codigoTipo == "FICHA_RUC" && resultado is FichaRucDto fichaRuc)
        {
            if (string.IsNullOrEmpty(expediente.RucEmpresa) && !string.IsNullOrEmpty(fichaRuc.Ruc))
            {
                expediente.RucEmpresa = fichaRuc.Ruc;
                actualizado = true;
            }
            if (string.IsNullOrEmpty(expediente.RazonSocialEmpresa) && !string.IsNullOrEmpty(fichaRuc.RazonSocial))
            {
                expediente.RazonSocialEmpresa = fichaRuc.RazonSocial;
                actualizado = true;
            }
        }

        if (actualizado)
        {
            await _expedienteRepo.UpdateAsync(expediente);
        }
    }

    public async Task<(string CodigoTipoDetectado, object Resultado, decimal? Confianza)> ClasificarYProcesarAutoAsync(
        Stream documentStream, string fileName,
        CancellationToken cancellationToken = default,
        IProgress<string>? progreso = null)
    {
        var clasificacion = await _documentIntelligence.ClasificarYProcesarAsync(
            documentStream, fileName, cancellationToken, progreso);

        var categoriaDetectada = clasificacion.CategoriaDetectada;

        if (categoriaDetectada == "other")
        {
            throw new InvalidOperationException(
                "El documento no corresponde a ningun tipo conocido. Suba un documento valido.");
        }

        if (clasificacion.ResultadoExtraccion == null)
        {
            throw new InvalidOperationException(
                $"El clasificador detecto el tipo ({categoriaDetectada}) pero no pudo extraer campos del documento.");
        }

        var confianza = ObtenerConfianzaDeResultado(clasificacion.ResultadoExtraccion);
        return (categoriaDetectada, clasificacion.ResultadoExtraccion, confianza);
    }

    public decimal? ObtenerConfianzaDeResultado(object resultado)
    {
        return resultado switch
        {
            DocumentoIdentidadDto dni => (decimal)dni.ConfianzaPromedio,
            VigenciaPoderDto vp => (decimal)vp.ConfianzaPromedio,
            BalanceGeneralDto bg => (decimal)bg.ConfianzaPromedio,
            EstadoResultadosDto er => er.ConfianzaPromedio,
            FichaRucDto fr => (decimal)fr.ConfianzaPromedio,
            _ => null
        };
    }
}
