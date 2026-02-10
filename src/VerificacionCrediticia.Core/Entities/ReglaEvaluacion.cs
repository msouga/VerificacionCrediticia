using VerificacionCrediticia.Core.Enums;

namespace VerificacionCrediticia.Core.Entities;

public class ReglaEvaluacion
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public string Campo { get; set; } = string.Empty;

    public OperadorComparacion Operador { get; set; }

    public decimal Valor { get; set; }

    public decimal Peso { get; set; }

    public ResultadoRegla Resultado { get; set; }

    public bool Activa { get; set; } = true;

    public int Orden { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
