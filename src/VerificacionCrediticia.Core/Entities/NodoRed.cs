using VerificacionCrediticia.Core.Enums;

namespace VerificacionCrediticia.Core.Entities;

public class NodoRed
{
    public string Identificador { get; set; } = string.Empty;
    public TipoNodo Tipo { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int NivelProfundidad { get; set; }
    public decimal? Score { get; set; }
    public string? NivelRiesgoTexto { get; set; }
    public EstadoCrediticio EstadoCredito { get; set; }
    public List<string> Alertas { get; set; } = new();
    public List<DeudaRegistrada> Deudas { get; set; } = new();
    public List<ConexionNodo> Conexiones { get; set; } = new();
}

public class ConexionNodo
{
    public string Identificador { get; set; } = string.Empty;
    public TipoNodo Tipo { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string TipoRelacion { get; set; } = string.Empty;
}
