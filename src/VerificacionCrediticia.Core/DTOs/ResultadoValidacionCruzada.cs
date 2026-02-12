using VerificacionCrediticia.Core.Enums;

namespace VerificacionCrediticia.Core.DTOs;

public class ResultadoValidacionCruzada
{
    public string Nombre { get; set; } = string.Empty;
    public bool Aprobada { get; set; }
    public ResultadoRegla Severidad { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public List<string> DocumentosInvolucrados { get; set; } = new();
}
