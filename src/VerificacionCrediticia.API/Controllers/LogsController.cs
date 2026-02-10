using Microsoft.AspNetCore.Mvc;
using VerificacionCrediticia.Core.DTOs;

namespace VerificacionCrediticia.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LogsController : ControllerBase
{
    private readonly ILogger<LogsController> _logger;

    public LogsController(ILogger<LogsController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Recibe logs del frontend y los registra en Serilog
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult RegistrarLog([FromBody] LogEntryDto entry)
    {
        var nivel = entry.Nivel?.ToUpperInvariant() ?? "INFORMATION";

        var mensaje = "[Frontend] [{Origen}] {Mensaje}";
        var args = new object[] { entry.Origen ?? "Unknown", entry.Mensaje ?? "" };

        switch (nivel)
        {
            case "ERROR":
                _logger.LogError(mensaje, args);
                if (!string.IsNullOrEmpty(entry.StackTrace))
                    _logger.LogError("  StackTrace: {StackTrace}", entry.StackTrace);
                break;
            case "WARNING":
                _logger.LogWarning(mensaje, args);
                break;
            case "DEBUG":
                _logger.LogDebug(mensaje, args);
                break;
            default:
                _logger.LogInformation(mensaje, args);
                break;
        }

        return NoContent();
    }
}
