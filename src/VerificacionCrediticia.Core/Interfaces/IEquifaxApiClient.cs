using VerificacionCrediticia.Core.DTOs;

namespace VerificacionCrediticia.Core.Interfaces;

public interface IEquifaxApiClient
{
    Task<ReporteCrediticioDto?> ConsultarReporteCrediticioAsync(
        string tipoDocumento,
        string numeroDocumento,
        CancellationToken cancellationToken = default);
}
