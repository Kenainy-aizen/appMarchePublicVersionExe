using Microsoft.EntityFrameworkCore;
using SIGMPOffline.Data.Repositories.Interfaces;
using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Data.Repositories.Impl;

/// <inheritdoc />
public class PpmRepository : Repository<PpmEntete>, IPpmRepository
{
    public PpmRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc />
    public async Task<PpmEntete?> GetWithDetailsAsync(string numPpm, CancellationToken cancellationToken = default)
        => await Set
            .Include(x => x.Details)
                .ThenInclude(x => x.Dossiers)
            .Include(x => x.Province)
            .FirstOrDefaultAsync(x => x.NumPPM == numPpm, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<PpmEntete>> ListByAnneeAsync(int annee, CancellationToken cancellationToken = default)
        => await Set
            .AsNoTracking()
            .Include(x => x.Details)
            .Where(x => x.AnneeExercice == annee)
            .OrderByDescending(x => x.DateElaboration)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<PpmEntete>> ListerAvecDetailsAsync(CancellationToken cancellationToken = default)
        => await Set
            .AsNoTracking()
            .Include(x => x.Details)
            .OrderByDescending(x => x.AnneeExercice)
            .ThenByDescending(x => x.DateElaboration)
            .ToListAsync(cancellationToken);
}
