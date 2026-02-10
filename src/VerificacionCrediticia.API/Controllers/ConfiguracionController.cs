using Microsoft.AspNetCore.Mvc;
using VerificacionCrediticia.Core.DTOs;
using VerificacionCrediticia.Core.Interfaces;

namespace VerificacionCrediticia.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfiguracionController : ControllerBase
{
    private readonly ITipoDocumentoRepository _tipoDocumentoRepo;
    private readonly ILogger<ConfiguracionController> _logger;

    public ConfiguracionController(
        ITipoDocumentoRepository tipoDocumentoRepo,
        ILogger<ConfiguracionController> logger)
    {
        _tipoDocumentoRepo = tipoDocumentoRepo;
        _logger = logger;
    }

    [HttpGet("tipos-documento")]
    [ProducesResponseType(typeof(List<TipoDocumentoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TipoDocumentoDto>>> GetTiposDocumento()
    {
        var tipos = await _tipoDocumentoRepo.GetAllAsync();
        var dtos = tipos
            .OrderBy(t => t.Orden)
            .Select(t => new TipoDocumentoDto
            {
                Id = t.Id,
                Nombre = t.Nombre,
                Codigo = t.Codigo,
                AnalyzerId = t.AnalyzerId,
                EsObligatorio = t.EsObligatorio,
                Activo = t.Activo,
                Orden = t.Orden,
                Descripcion = t.Descripcion
            }).ToList();

        return Ok(dtos);
    }

    [HttpPut("tipos-documento/{id}")]
    [ProducesResponseType(typeof(TipoDocumentoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TipoDocumentoDto>> ActualizarTipoDocumento(
        int id, [FromBody] ActualizarTipoDocumentoRequest request)
    {
        var tipo = await _tipoDocumentoRepo.GetByIdAsync(id);
        if (tipo == null)
            return NotFound(new { message = $"Tipo de documento {id} no encontrado" });

        tipo.EsObligatorio = request.EsObligatorio;
        tipo.Activo = request.Activo;
        tipo.Orden = request.Orden;
        tipo.Descripcion = request.Descripcion;
        tipo.AnalyzerId = request.AnalyzerId;

        await _tipoDocumentoRepo.UpdateAsync(tipo);

        _logger.LogInformation(
            "Tipo de documento {Id} ({Codigo}) actualizado: Obligatorio={Obligatorio}, Activo={Activo}, Orden={Orden}",
            tipo.Id, tipo.Codigo, tipo.EsObligatorio, tipo.Activo, tipo.Orden);

        return Ok(new TipoDocumentoDto
        {
            Id = tipo.Id,
            Nombre = tipo.Nombre,
            Codigo = tipo.Codigo,
            AnalyzerId = tipo.AnalyzerId,
            EsObligatorio = tipo.EsObligatorio,
            Activo = tipo.Activo,
            Orden = tipo.Orden,
            Descripcion = tipo.Descripcion
        });
    }
}
