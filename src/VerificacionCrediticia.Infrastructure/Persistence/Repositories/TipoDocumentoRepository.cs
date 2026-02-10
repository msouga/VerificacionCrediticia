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

    public async Task<TipoDocumento?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.TiposDocumento
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<TipoDocumento?> GetByCodigoAsync(string codigo, CancellationToken cancellationToken = default)
    {
        return await _context.TiposDocumento
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Codigo == codigo, cancellationToken);
    }

    public async Task<List<TipoDocumento>> GetActivosAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TiposDocumento
            .Where(t => t.Activo)
            .AsNoTracking()
            .OrderBy(t => t.Orden)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<TipoDocumento>> GetObligatoriosAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TiposDocumento
            .Where(t => t.Activo && t.EsObligatorio)
            .AsNoTracking()
            .OrderBy(t => t.Orden)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<TipoDocumento>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TiposDocumento
            .AsNoTracking()
            .OrderBy(t => t.Orden)
            .ToListAsync(cancellationToken);
    }

    public async Task<TipoDocumento> CreateAsync(TipoDocumento tipoDocumento, CancellationToken cancellationToken = default)
    {
        _context.TiposDocumento.Add(tipoDocumento);
        await _context.SaveChangesAsync(cancellationToken);
        return tipoDocumento;
    }

    public async Task UpdateAsync(TipoDocumento tipoDocumento, CancellationToken cancellationToken = default)
    {
        _context.Entry(tipoDocumento).State = EntityState.Modified;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var tipoDocumento = await _context.TiposDocumento.FindAsync([id], cancellationToken);
        if (tipoDocumento != null)
        {
            _context.TiposDocumento.Remove(tipoDocumento);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.TiposDocumento
            .AnyAsync(t => t.Id == id, cancellationToken);
    }
}
