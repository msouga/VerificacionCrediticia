using Microsoft.EntityFrameworkCore;
using VerificacionCrediticia.Core.Entities;
using VerificacionCrediticia.Core.Interfaces;

namespace VerificacionCrediticia.Infrastructure.Persistence.Repositories;

public class ExpedienteRepository : IExpedienteRepository
{
    private readonly ApplicationDbContext _context;

    public ExpedienteRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Expediente?> GetByIdAsync(int id)
    {
        return await _context.Expedientes
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<Expediente?> GetByIdWithDocumentosAsync(int id)
    {
        return await _context.Expedientes
            .Include(e => e.Documentos)
                .ThenInclude(d => d.TipoDocumento)
            .Include(e => e.ResultadoEvaluacion)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<Expediente?> GetByDniAsync(string dni)
    {
        return await _context.Expedientes
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.DniSolicitante == dni);
    }

    public async Task<Expediente> CreateAsync(Expediente expediente)
    {
        _context.Expedientes.Add(expediente);
        await _context.SaveChangesAsync();
        return expediente;
    }

    public async Task UpdateAsync(Expediente expediente)
    {
        _context.Entry(expediente).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var expediente = await _context.Expedientes.FindAsync(id);
        if (expediente != null)
        {
            _context.Expedientes.Remove(expediente);
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteManyAsync(List<int> ids)
    {
        var expedientes = await _context.Expedientes
            .Where(e => ids.Contains(e.Id))
            .ToListAsync();

        if (expedientes.Count > 0)
        {
            _context.Expedientes.RemoveRange(expedientes);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Expedientes
            .AnyAsync(e => e.Id == id);
    }

    public async Task<List<Expediente>> GetAllAsync()
    {
        return await _context.Expedientes
            .AsNoTracking()
            .OrderByDescending(e => e.FechaCreacion)
            .ToListAsync();
    }

    public async Task<(List<Expediente> Items, int Total)> GetPaginadoAsync(int pagina, int tamanoPagina)
    {
        var query = _context.Expedientes.AsNoTracking();

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(e => e.FechaCreacion)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Include(e => e.Documentos)
                .ThenInclude(d => d.TipoDocumento)
            .ToListAsync();

        return (items, total);
    }
}
