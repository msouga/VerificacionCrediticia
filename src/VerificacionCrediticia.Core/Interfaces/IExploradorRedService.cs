using VerificacionCrediticia.Core.DTOs;

namespace VerificacionCrediticia.Core.Interfaces;

public interface IExploradorRedService
{
    Task<ResultadoExploracionDto> ExplorarRedAsync(
        string dniSolicitante,
        string rucEmpresaSolicitante,
        int profundidadMaxima = 2,
        CancellationToken cancellationToken = default);
}
