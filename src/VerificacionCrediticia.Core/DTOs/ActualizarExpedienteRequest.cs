using System.ComponentModel.DataAnnotations;

namespace VerificacionCrediticia.Core.DTOs;

public class ActualizarExpedienteRequest
{
    [Required(ErrorMessage = "La descripcion es obligatoria")]
    [MaxLength(40, ErrorMessage = "La descripcion no puede exceder 40 caracteres")]
    public string Descripcion { get; set; } = string.Empty;
}
