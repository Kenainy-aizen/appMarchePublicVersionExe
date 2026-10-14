using Microsoft.EntityFrameworkCore;
using SIGMPOffline.Data.Repositories.Interfaces;
using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Data.Repositories.Impl;

/// <inheritdoc />
public class DossierDaoRepository : Repository<DossierDao>, IDossierDaoRepository
{
    public DossierDaoRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc />
    public async Task<DossierDao?> GetWithCandidatsAsync(string refDossier, CancellationToken cancellationToken = default)
        => await Set
            .Include(x => x.Candidats)
            .FirstOrDefaultAsync(x => x.RefDossier == refDossier, cancellationToken);

    /// <inheritdoc />
    public async Task<DossierDao?> GetFullAsync(string refDossier, CancellationToken cancellationToken = default)
        => await Set
            .Include(x => x.PpmDetail)
            .Include(x => x.Acheteurs)
            .Include(x => x.Candidats)
            .Include(x => x.Marche)
            .FirstOrDefaultAsync(x => x.RefDossier == refDossier, cancellationToken);
}
