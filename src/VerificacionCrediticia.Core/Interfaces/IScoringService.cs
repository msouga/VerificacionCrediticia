using VerificacionCrediticia.Core.DTOs;

namespace VerificacionCrediticia.Core.Interfaces;

public interface IScoringService
{
    ResultadoEvaluacionDto EvaluarRed(ResultadoExploracionDto exploracion);
}
