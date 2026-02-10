namespace VerificacionCrediticia.Core.DTOs;

/// <summary>
/// Resultado de la validacion de un DNI contra RENIEC
/// </summary>
public class ReniecValidacionDto
{
    public bool DniValido { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public string? NombresReniec { get; set; }
    public string? ApellidosReniec { get; set; }
}
