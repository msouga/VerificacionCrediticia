namespace VerificacionCrediticia.Core.DTOs;

public class ListaExpedientesResponse
{
    public List<ExpedienteResumenDto> Items { get; set; } = new();
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int TamanoPagina { get; set; }
}
