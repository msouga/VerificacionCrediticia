using VerificacionCrediticia.Core.DTOs;

namespace VerificacionCrediticia.Core.Interfaces;

public interface IReniecValidationService
{
    Task<ReniecValidacionDto> ValidarDniAsync(string numeroDni, CancellationToken cancellationToken = default);
}
