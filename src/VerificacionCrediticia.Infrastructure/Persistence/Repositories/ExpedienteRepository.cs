using Microsoft.EntityFrameworkCore;
using VerificacionCrediticia.Core.DTOs;
using VerificacionCrediticia.Core.Entities;
using VerificacionCrediticia.Core.Enums;
using VerificacionCrediticia.Core.Interfaces;
using Recomendacion = VerificacionCrediticia.Core.Enums.Recomendacion;

namespace VerificacionCrediticia.Infrastructure.Persistence.Repositories;

public class ExpedienteRepository : IExpedienteRepository
{
    private readonly ApplicationDbContext _context;

    public ExpedienteRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Expediente?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Expedientes
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<Expediente?> GetByIdTrackingAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Expedientes
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<Expediente?> GetByIdWithDocumentosAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Expedientes
            .Include(e => e.Documentos)
                .ThenInclude(d => d.TipoDocumento)
            .Include(e => e.ResultadoEvaluacion)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<Expediente?> GetByIdWithDocumentosTrackingAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Expedientes
            .Include(e => e.Documentos)
                .ThenInclude(d => d.TipoDocumento)
            .Include(e => e.ResultadoEvaluacion)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<Expediente?> GetByDniAsync(string dni, CancellationToken cancellationToken = default)
    {
        return await _context.Expedientes
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.DniSolicitante == dni, cancellationToken);
    }

    public async Task<Expediente> CreateAsync(Expediente expediente, CancellationToken cancellationToken = default)
    {
        _context.Expedientes.Add(expediente);
        await _context.SaveChangesAsync(cancellationToken);
        return expediente;
    }

    public async Task UpdateAsync(Expediente expediente, CancellationToken cancellationToken = default)
    {
        var entry = _context.Entry(expediente);
        if (entry.State == EntityState.Detached)
        {
            _context.Update(expediente);
        }
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var expediente = await _context.Expedientes.FindAsync([id], cancellationToken);
        if (expediente != null)
        {
            _context.Expedientes.Remove(expediente);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteManyAsync(List<int> ids, CancellationToken cancellationToken = default)
    {
        var expedientes = await _context.Expedientes
            .Where(e => ids.Contains(e.Id))
            .ToListAsync(cancellationToken);

        if (expedientes.Count > 0)
        {
            _context.Expedientes.RemoveRange(expedientes);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Expedientes
            .AnyAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<List<Expediente>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Expedientes
            .AsNoTracking()
            .OrderByDescending(e => e.FechaCreacion)
            .ToListAsync(cancellationToken);
    }

    public async Task<(List<Expediente> Items, int Total)> GetPaginadoAsync(int pagina, int tamanoPagina, CancellationToken cancellationToken = default)
    {
        var query = _context.Expedientes.AsNoTracking();

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(e => e.FechaCreacion)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<(List<ExpedienteResumenDto> Items, int Total)> GetPaginadoResumenAsync(
        int pagina, int tamanoPagina, List<int> tipoDocumentoObligatorioIds,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Expedientes.AsNoTracking();

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(e => e.FechaCreacion)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(e => new ExpedienteResumenDto
            {
                Id = e.Id,
                Descripcion = e.Descripcion,
                DniSolicitante = e.DniSolicitante,
                NombresSolicitante = e.NombresSolicitante,
                RucEmpresa = e.RucEmpresa,
                RazonSocialEmpresa = e.RazonSocialEmpresa,
                Estado = e.Estado,
                FechaCreacion = e.FechaCreacion,
                DocumentosObligatoriosCompletos = e.Documentos.Count(d =>
                    d.Estado == EstadoDocumento.Procesado
                    && d.TipoDocumentoId.HasValue
                    && tipoDocumentoObligatorioIds.Contains(d.TipoDocumentoId.Value)),
                TotalDocumentosObligatorios = tipoDocumentoObligatorioIds.Count,
                Recomendacion = e.ResultadoEvaluacion != null ? e.ResultadoEvaluacion.Recomendacion : null,
                ScoreFinal = e.ResultadoEvaluacion != null ? e.ResultadoEvaluacion.ScoreFinal : null
            })
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<EstadisticasExpedientesDto> GetEstadisticasAsync(CancellationToken cancellationToken = default)
    {
        var totalExpedientes = await _context.Expedientes.CountAsync(cancellationToken);

        var evaluados = await _context.Expedientes
            .Where(e => e.Estado == EstadoExpediente.Evaluado)
            .CountAsync(cancellationToken);

        var enProceso = await _context.Expedientes
            .Where(e => e.Estado != EstadoExpediente.Evaluado)
            .CountAsync(cancellationToken);

        var resultados = await _context.Set<ResultadoEvaluacionPersistido>()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var aprobados = resultados.Count(r => r.Recomendacion == Recomendacion.Aprobar);
        var enRevision = resultados.Count(r => r.Recomendacion == Recomendacion.RevisarManualmente);
        var rechazados = resultados.Count(r => r.Recomendacion == Recomendacion.Rechazar);
        var scorePromedio = resultados.Count > 0 ? resultados.Average(r => r.ScoreFinal) : 0m;

        var recientes = await _context.Expedientes
            .AsNoTracking()
            .Include(e => e.ResultadoEvaluacion)
            .Where(e => e.Estado == EstadoExpediente.Evaluado && e.ResultadoEvaluacion != null)
            .OrderByDescending(e => e.ResultadoEvaluacion!.FechaEvaluacion)
            .Take(5)
            .Select(e => new ExpedienteEvaluadoResumenDto
            {
                Id = e.Id,
                Descripcion = e.Descripcion,
                DniSolicitante = e.DniSolicitante,
                NombresSolicitante = e.NombresSolicitante,
                ApellidosSolicitante = e.ApellidosSolicitante,
                RucEmpresa = e.RucEmpresa,
                RazonSocialEmpresa = e.RazonSocialEmpresa,
                ScoreFinal = e.ResultadoEvaluacion!.ScoreFinal,
                Recomendacion = (int)e.ResultadoEvaluacion.Recomendacion,
                FechaEvaluacion = e.ResultadoEvaluacion.FechaEvaluacion
            })
            .ToListAsync(cancellationToken);

        return new EstadisticasExpedientesDto
        {
            TotalExpedientes = totalExpedientes,
            Evaluados = evaluados,
            EnProceso = enProceso,
            Aprobados = aprobados,
            EnRevision = enRevision,
            Rechazados = rechazados,
            ScorePromedio = Math.Round(scorePromedio, 1),
            Recientes = recientes
        };
    }
}
