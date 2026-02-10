using Microsoft.EntityFrameworkCore;
using VerificacionCrediticia.Core.Entities;
using VerificacionCrediticia.Core.Interfaces;

namespace VerificacionCrediticia.Infrastructure.Persistence.Repositories;

public class ReglaEvaluacionRepository : IReglaEvaluacionRepository
{
    private readonly ApplicationDbContext _context;

    public ReglaEvaluacionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ReglaEvaluacion?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.ReglasEvaluacion
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<List<ReglaEvaluacion>> GetActivasAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ReglasEvaluacion
            .Where(r => r.Activa)
            .AsNoTracking()
            .OrderBy(r => r.Orden)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ReglaEvaluacion>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ReglasEvaluacion
            .AsNoTracking()
            .OrderBy(r => r.Orden)
            .ToListAsync(cancellationToken);
    }

    public async Task<ReglaEvaluacion> CreateAsync(ReglaEvaluacion regla, CancellationToken cancellationToken = default)
    {
        _context.ReglasEvaluacion.Add(regla);
        await _context.SaveChangesAsync(cancellationToken);
        return regla;
    }

    public async Task UpdateAsync(ReglaEvaluacion regla, CancellationToken cancellationToken = default)
    {
        _context.Entry(regla).State = EntityState.Modified;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var regla = await _context.ReglasEvaluacion.FindAsync([id], cancellationToken);
        if (regla != null)
        {
            _context.ReglasEvaluacion.Remove(regla);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.ReglasEvaluacion
            .AnyAsync(r => r.Id == id, cancellationToken);
    }
}
