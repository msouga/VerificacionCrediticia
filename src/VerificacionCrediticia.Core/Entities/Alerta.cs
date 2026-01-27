using VerificacionCrediticia.Core.Enums;

namespace VerificacionCrediticia.Core.Entities;

public class Alerta
{
    public TipoAlerta Tipo { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public Severidad Severidad { get; set; }
    public int NivelProfundidad { get; set; }
    public string? IdentificadorEntidad { get; set; }
    public TipoNodo? TipoEntidad { get; set; }
}
