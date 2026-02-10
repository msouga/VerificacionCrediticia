namespace VerificacionCrediticia.Core.DTOs;

public class ReglaEvaluacionDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Campo { get; set; } = string.Empty;
    public int Operador { get; set; }
    public decimal Valor { get; set; }
    public decimal Peso { get; set; }
    public int Resultado { get; set; }
    public bool Activa { get; set; }
    public int Orden { get; set; }
}

public class CrearReglaRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Campo { get; set; } = string.Empty;
    public int Operador { get; set; }
    public decimal Valor { get; set; }
    public decimal Peso { get; set; }
    public int Resultado { get; set; }
    public int Orden { get; set; }
}

public class ActualizarReglaRequest
{
    public string? Descripcion { get; set; }
    public int Operador { get; set; }
    public decimal Valor { get; set; }
    public decimal Peso { get; set; }
    public int Resultado { get; set; }
    public bool Activa { get; set; }
    public int Orden { get; set; }
}
