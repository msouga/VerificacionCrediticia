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

    public async Task<ReglaEvaluacion?> GetByIdAsync(int id)
    {
        return await _context.ReglasEvaluacion
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<List<ReglaEvaluacion>> GetActivasAsync()
    {
        return await _context.ReglasEvaluacion
            .Where(r => r.Activa)
            .AsNoTracking()
            .OrderBy(r => r.Orden)
            .ToListAsync();
    }

    public async Task<List<ReglaEvaluacion>> GetAllAsync()
    {
        return await _context.ReglasEvaluacion
            .AsNoTracking()
            .OrderBy(r => r.Orden)
            .ToListAsync();
    }

    public async Task<ReglaEvaluacion> CreateAsync(ReglaEvaluacion regla)
    {
        _context.ReglasEvaluacion.Add(regla);
        await _context.SaveChangesAsync();
        return regla;
    }

    public async Task UpdateAsync(ReglaEvaluacion regla)
    {
        _context.Entry(regla).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var regla = await _context.ReglasEvaluacion.FindAsync(id);
        if (regla != null)
        {
            _context.ReglasEvaluacion.Remove(regla);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.ReglasEvaluacion
            .AnyAsync(r => r.Id == id);
    }
}