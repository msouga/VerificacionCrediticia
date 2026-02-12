using VerificacionCrediticia.Core.Entities;

namespace VerificacionCrediticia.Core.Interfaces;

public interface IParametroLineaCreditoRepository
{
    Task<ParametroLineaCredito> GetAsync(CancellationToken cancellationToken = default);
}
