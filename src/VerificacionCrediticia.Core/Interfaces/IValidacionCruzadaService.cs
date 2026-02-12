using VerificacionCrediticia.Core.DTOs;
using VerificacionCrediticia.Core.Entities;

namespace VerificacionCrediticia.Core.Interfaces;

public interface IValidacionCruzadaService
{
    List<ResultadoValidacionCruzada> ValidarDocumentos(Expediente expediente, List<DocumentoProcesado> documentos);
}
