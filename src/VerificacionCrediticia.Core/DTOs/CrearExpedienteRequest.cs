using System.ComponentModel.DataAnnotations;

namespace VerificacionCrediticia.Core.DTOs;

public class CrearExpedienteRequest
{
    [Required(ErrorMessage = "El DNI del solicitante es obligatorio")]
    [StringLength(8, MinimumLength = 8, ErrorMessage = "El DNI debe tener 8 dígitos")]
    [RegularExpression(@"^\d{8}$", ErrorMessage = "El DNI debe contener solo dígitos")]
    public string DniSolicitante { get; set; } = string.Empty;

    [StringLength(11, MinimumLength = 11, ErrorMessage = "El RUC debe tener 11 dígitos")]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "El RUC debe contener solo dígitos")]
    public string? RucEmpresa { get; set; }
}