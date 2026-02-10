using Microsoft.EntityFrameworkCore;
using VerificacionCrediticia.Core.Entities;
using VerificacionCrediticia.Core.Interfaces;

namespace VerificacionCrediticia.Infrastructure.Persistence.Repositories;

public class TipoDocumentoRepository : ITipoDocumentoRepository
{
    private readonly ApplicationDbContext _context;

    public TipoDocumentoRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TipoDocumento?> GetByIdAsync(int id)
    {
        return await _context.TiposDocumento
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<TipoDocumento?> GetByCodigoAsync(string codigo)
    {
        return await _context.TiposDocumento
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Codigo == codigo);
    }

    public async Task<List<TipoDocumento>> GetActivosAsync()
    {
        return await _context.TiposDocumento
            .Where(t => t.Activo)
            .AsNoTracking()
            .OrderBy(t => t.Orden)
            .ToListAsync();
    }

    public async Task<List<TipoDocumento>> GetObligatoriosAsync()
    {
        return await _context.TiposDocumento
            .Where(t => t.Activo && t.EsObligatorio)
            .AsNoTracking()
            .OrderBy(t => t.Orden)
            .ToListAsync();
    }

    public async Task<List<TipoDocumento>> GetAllAsync()
    {
        return await _context.TiposDocumento
            .AsNoTracking()
            .OrderBy(t => t.Orden)
            .ToListAsync();
    }

    public async Task<TipoDocumento> CreateAsync(TipoDocumento tipoDocumento)
    {
        _context.TiposDocumento.Add(tipoDocumento);
        await _context.SaveChangesAsync();
        return tipoDocumento;
    }

    public async Task UpdateAsync(TipoDocumento tipoDocumento)
    {
        _context.Entry(tipoDocumento).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var tipoDocumento = await _context.TiposDocumento.FindAsync(id);
        if (tipoDocumento != null)
        {
            _context.TiposDocumento.Remove(tipoDocumento);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.TiposDocumento
            .AnyAsync(t => t.Id == id);
    }
}