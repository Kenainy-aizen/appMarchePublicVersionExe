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
            .FirstOrDefaultAsync(x => x.NumPPM == numPpm, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<PpmEntete>> ListByAnneeAsync(int annee, CancellationToken cancellationToken = default)
        => await Set
            .AsNoTracking()
            .Where(x => x.AnneeExercice == annee)
            .OrderByDescending(x => x.DateElaboration)
            .ToListAsync(cancellationToken);
}
