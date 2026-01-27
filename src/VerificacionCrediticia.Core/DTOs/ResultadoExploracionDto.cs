using VerificacionCrediticia.Core.Entities;

namespace VerificacionCrediticia.Core.DTOs;

public class ResultadoExploracionDto
{
    public string DniSolicitante { get; set; } = string.Empty;
    public string RucEmpresa { get; set; } = string.Empty;
    public Dictionary<string, NodoRed> Grafo { get; set; } = new();
    public int TotalNodos { get; set; }
    public int TotalPersonas { get; set; }
    public int TotalEmpresas { get; set; }
    public DateTime FechaConsulta { get; set; }
}
