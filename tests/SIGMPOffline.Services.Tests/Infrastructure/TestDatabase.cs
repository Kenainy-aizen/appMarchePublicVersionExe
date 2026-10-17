using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SIGMPOffline.Data;
using SIGMPOffline.Data.Database;
using SIGMPOffline.Data.Repositories;
using SIGMPOffline.Data.Repositories.Interfaces;

namespace SIGMPOffline.Services.Tests.Infrastructure;

/// <summary>
/// Base SQLite en mémoire partagée pour les tests d'intégration.
/// Le schéma est créé via <c>EnsureCreated</c>.
/// </summary>
public sealed class TestDatabase : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private TestDatabase(SqliteConnection connection, AppDbContext context, ProvinceAccessor provinceAccessor)
    {
        _connection = connection;
        Context = context;
        ProvinceAccessor = provinceAccessor;
        UnitOfWork = new UnitOfWork(context);
    }

    /// <summary>Contexte EF Core de test.</summary>
    public AppDbContext Context { get; }

    /// <summary>Unité de travail de test.</summary>
    public IUnitOfWork UnitOfWork { get; }

    /// <summary>
    /// Province active pour les tests. Laisser à <c>null</c> pour un accès
    /// non filtré (comportement des tests mono-province existants).
    /// </summary>
    public ProvinceAccessor ProvinceAccessor { get; }

    /// <summary>Crée une base en mémoire prête à l'emploi.</summary>
    public static async Task<TestDatabase> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var provinceAccessor = new ProvinceAccessor();
        var context = new AppDbContext(options, provinceAccessor);
        await context.Database.EnsureCreatedAsync();

        return new TestDatabase(connection, context, provinceAccessor);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
