using Microsoft.AspNetCore.Mvc;
using VerificacionCrediticia.Core.DTOs;
using VerificacionCrediticia.Core.Entities;
using VerificacionCrediticia.Core.Enums;
using VerificacionCrediticia.Core.Interfaces;

namespace VerificacionCrediticia.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfiguracionController : ControllerBase
{
    private readonly ITipoDocumentoRepository _tipoDocumentoRepo;
    private readonly IReglaEvaluacionRepository _reglaRepo;
    private readonly ILogger<ConfiguracionController> _logger;

    public ConfiguracionController(
        ITipoDocumentoRepository tipoDocumentoRepo,
        IReglaEvaluacionRepository reglaRepo,
        ILogger<ConfiguracionController> logger)
    {
        _tipoDocumentoRepo = tipoDocumentoRepo;
        _reglaRepo = reglaRepo;
        _logger = logger;
    }

    // --- Tipos de Documento ---

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

    // --- Reglas de Evaluacion ---

    [HttpGet("reglas")]
    [ProducesResponseType(typeof(List<ReglaEvaluacionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ReglaEvaluacionDto>>> GetReglas()
    {
        var reglas = await _reglaRepo.GetAllAsync();
        var dtos = reglas
            .OrderBy(r => r.Orden)
            .Select(MapToDto)
            .ToList();

        return Ok(dtos);
    }

    [HttpGet("reglas/{id}")]
    [ProducesResponseType(typeof(ReglaEvaluacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReglaEvaluacionDto>> GetRegla(int id)
    {
        var regla = await _reglaRepo.GetByIdAsync(id);
        if (regla == null)
            return NotFound(new { message = $"Regla {id} no encontrada" });

        return Ok(MapToDto(regla));
    }

    [HttpPost("reglas")]
    [ProducesResponseType(typeof(ReglaEvaluacionDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ReglaEvaluacionDto>> CrearRegla([FromBody] CrearReglaRequest request)
    {
        var regla = new ReglaEvaluacion
        {
            Nombre = request.Nombre,
            Descripcion = request.Descripcion,
            Campo = request.Campo,
            Operador = (OperadorComparacion)request.Operador,
            Valor = request.Valor,
            Peso = request.Peso,
            Resultado = (ResultadoRegla)request.Resultado,
            Activa = true,
            Orden = request.Orden
        };

        var creada = await _reglaRepo.CreateAsync(regla);

        _logger.LogInformation("Regla creada: {Id} - {Nombre}", creada.Id, creada.Nombre);

        return CreatedAtAction(nameof(GetRegla), new { id = creada.Id }, MapToDto(creada));
    }

    [HttpPut("reglas/{id}")]
    [ProducesResponseType(typeof(ReglaEvaluacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReglaEvaluacionDto>> ActualizarRegla(
        int id, [FromBody] ActualizarReglaRequest request)
    {
        var regla = await _reglaRepo.GetByIdAsync(id);
        if (regla == null)
            return NotFound(new { message = $"Regla {id} no encontrada" });

        regla.Descripcion = request.Descripcion;
        regla.Operador = (OperadorComparacion)request.Operador;
        regla.Valor = request.Valor;
        regla.Peso = request.Peso;
        regla.Resultado = (ResultadoRegla)request.Resultado;
        regla.Activa = request.Activa;
        regla.Orden = request.Orden;

        await _reglaRepo.UpdateAsync(regla);

        _logger.LogInformation("Regla actualizada: {Id} - {Nombre}", regla.Id, regla.Nombre);

        return Ok(MapToDto(regla));
    }

    [HttpDelete("reglas/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EliminarRegla(int id)
    {
        var existe = await _reglaRepo.ExistsAsync(id);
        if (!existe)
            return NotFound(new { message = $"Regla {id} no encontrada" });

        await _reglaRepo.DeleteAsync(id);

        _logger.LogInformation("Regla eliminada: {Id}", id);

        return NoContent();
    }

    private static ReglaEvaluacionDto MapToDto(ReglaEvaluacion regla) => new()
    {
        Id = regla.Id,
        Nombre = regla.Nombre,
        Descripcion = regla.Descripcion,
        Campo = regla.Campo,
        Operador = (int)regla.Operador,
        Valor = regla.Valor,
        Peso = regla.Peso,
        Resultado = (int)regla.Resultado,
        Activa = regla.Activa,
        Orden = regla.Orden
    };
}
