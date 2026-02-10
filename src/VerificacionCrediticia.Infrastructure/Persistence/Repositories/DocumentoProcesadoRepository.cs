using Microsoft.EntityFrameworkCore;
using VerificacionCrediticia.Core.Entities;
using VerificacionCrediticia.Core.Interfaces;

namespace VerificacionCrediticia.Infrastructure.Persistence.Repositories;

public class DocumentoProcesadoRepository : IDocumentoProcesadoRepository
{
    private readonly ApplicationDbContext _context;

    public DocumentoProcesadoRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DocumentoProcesado?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.DocumentosProcesados
            .Include(d => d.TipoDocumento)
            .Include(d => d.Expediente)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<DocumentoProcesado?> GetByExpedienteAndTipoAsync(int expedienteId, int tipoDocumentoId, CancellationToken cancellationToken = default)
    {
        return await _context.DocumentosProcesados
            .Include(d => d.TipoDocumento)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.ExpedienteId == expedienteId && d.TipoDocumentoId == tipoDocumentoId, cancellationToken);
    }

    public async Task<List<DocumentoProcesado>> GetByExpedienteIdAsync(int expedienteId, CancellationToken cancellationToken = default)
    {
        return await _context.DocumentosProcesados
            .Include(d => d.TipoDocumento)
            .Where(d => d.ExpedienteId == expedienteId)
            .AsNoTracking()
            .OrderBy(d => d.TipoDocumento.Orden)
            .ToListAsync(cancellationToken);
    }

    public async Task<DocumentoProcesado> CreateAsync(DocumentoProcesado documento, CancellationToken cancellationToken = default)
    {
        _context.DocumentosProcesados.Add(documento);
        await _context.SaveChangesAsync(cancellationToken);
        return documento;
    }

    public async Task UpdateAsync(DocumentoProcesado documento, CancellationToken cancellationToken = default)
    {
        _context.Entry(documento).State = EntityState.Modified;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var documento = await _context.DocumentosProcesados.FindAsync([id], cancellationToken);
        if (documento != null)
        {
            _context.DocumentosProcesados.Remove(documento);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.DocumentosProcesados
            .AnyAsync(d => d.Id == id, cancellationToken);
    }
}
