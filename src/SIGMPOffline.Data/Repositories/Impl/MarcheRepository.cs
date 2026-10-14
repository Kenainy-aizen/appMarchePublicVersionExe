using Microsoft.EntityFrameworkCore;
using SIGMPOffline.Data.Repositories.Interfaces;
using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Data.Repositories.Impl;

/// <inheritdoc />
public class MarcheRepository : Repository<Marche>, IMarcheRepository
{
    public MarcheRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc />
    public async Task<Marche?> GetWithExecutionAsync(string refMarche, CancellationToken cancellationToken = default)
        => await Set
            .Include(x => x.Dossier)
            .Include(x => x.Titulaire)
            .Include(x => x.Elements)
            .Include(x => x.Suivi)
            .Include(x => x.Avenants)
            .FirstOrDefaultAsync(x => x.RefMarche == refMarche, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Marche>> ListWithExecutionAsync(CancellationToken cancellationToken = default)
        => await Set
            .AsNoTracking()
            .Include(x => x.Dossier)
            .Include(x => x.Titulaire)
            .Include(x => x.Suivi)
            .Include(x => x.Avenants)
            .OrderBy(x => x.RefMarche)
            .ToListAsync(cancellationToken);
}
