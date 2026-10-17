using Microsoft.EntityFrameworkCore;
using SIGMPOffline.Data.Repositories.Interfaces;
using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Data.Repositories.Impl;

/// <inheritdoc />
public class ConfigurationAppRepository : Repository<ConfigurationApp>, IConfigurationAppRepository
{
    public ConfigurationAppRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc />
    public async Task<ConfigurationApp?> GetConfigurationAsync(CancellationToken cancellationToken = default)
        => await Set.FirstOrDefaultAsync(cancellationToken);
}
