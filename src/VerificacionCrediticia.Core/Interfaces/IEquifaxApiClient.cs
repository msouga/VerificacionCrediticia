using VerificacionCrediticia.Core.Entities;

namespace VerificacionCrediticia.Core.Interfaces;

public interface IEquifaxApiClient
{
    Task<Persona?> ConsultarPersonaAsync(string dni, CancellationToken cancellationToken = default);
    Task<Empresa?> ConsultarEmpresaAsync(string ruc, CancellationToken cancellationToken = default);
    Task<List<RelacionSocietaria>> ObtenerEmpresasDondeEsSocioAsync(string dni, CancellationToken cancellationToken = default);
    Task<List<RelacionSocietaria>> ObtenerSociosDeEmpresaAsync(string ruc, CancellationToken cancellationToken = default);
}
