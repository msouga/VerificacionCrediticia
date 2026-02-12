using Microsoft.EntityFrameworkCore;
using VerificacionCrediticia.Core.DTOs;
using VerificacionCrediticia.Core.Entities;
using VerificacionCrediticia.Core.Enums;
using VerificacionCrediticia.Core.Interfaces;

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
                TotalDocumentosObligatorios = tipoDocumentoObligatorioIds.Count
            })
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
