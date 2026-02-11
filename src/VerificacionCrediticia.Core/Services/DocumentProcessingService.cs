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

    public async Task<object> ProcesarSegunTipoAsync(
        string codigoTipo, Stream documentStream, string fileName,
        CancellationToken cancellationToken = default,
        IProgress<string>? progreso = null)
    {
        return codigoTipo switch
        {
            "DNI" => await _documentIntelligence.ProcesarDocumentoIdentidadAsync(documentStream, fileName, cancellationToken),
            "VIGENCIA_PODER" => await _documentIntelligence.ProcesarVigenciaPoderAsync(documentStream, fileName, cancellationToken, progreso),
            "BALANCE_GENERAL" => await _documentIntelligence.ProcesarBalanceGeneralAsync(documentStream, fileName, cancellationToken, progreso),
            "ESTADO_RESULTADOS" => await _documentIntelligence.ProcesarEstadoResultadosAsync(documentStream, fileName, cancellationToken, progreso),
            "FICHA_RUC" => await _documentIntelligence.ProcesarFichaRucAsync(documentStream, fileName, cancellationToken, progreso),
            _ => throw new NotSupportedException($"Tipo de documento '{codigoTipo}' no tiene procesamiento implementado")
        };
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
