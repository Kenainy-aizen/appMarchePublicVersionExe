using Microsoft.EntityFrameworkCore;
using SIGMPOffline.Data.Repositories.Interfaces;
using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Data.Repositories.Impl;

/// <inheritdoc />
public class ProvinceRepository : Repository<Province>, IProvinceRepository
{
    public ProvinceRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Province>> ListerAsync(CancellationToken cancellationToken = default)
        => await Set
            .AsNoTracking()
            .OrderBy(p => p.NomProvince)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<Province?> GetByNomAsync(string nom, CancellationToken cancellationToken = default)
        => await Set.FirstOrDefaultAsync(p => p.NomProvince == nom, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> NomExisteAsync(
        string nom,
        int? saufId = null,
        CancellationToken cancellationToken = default)
        => await Set.AnyAsync(
            p => p.NomProvince == nom && (saufId == null || p.IdProvince != saufId),
            cancellationToken);
}
