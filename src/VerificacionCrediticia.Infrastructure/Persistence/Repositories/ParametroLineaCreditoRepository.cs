using Microsoft.EntityFrameworkCore;
using VerificacionCrediticia.Core.Entities;
using VerificacionCrediticia.Core.Interfaces;

namespace VerificacionCrediticia.Infrastructure.Persistence.Repositories;

public class ParametroLineaCreditoRepository : IParametroLineaCreditoRepository
{
    private readonly ApplicationDbContext _context;

    public ParametroLineaCreditoRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ParametroLineaCredito> GetAsync(CancellationToken cancellationToken = default)
    {
        var parametros = await _context.ParametrosLineaCredito
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        return parametros ?? new ParametroLineaCredito
        {
            PorcentajeCapitalTrabajo = 20m,
            PorcentajePatrimonio = 30m,
            PorcentajeUtilidadNeta = 100m
        };
    }
}
