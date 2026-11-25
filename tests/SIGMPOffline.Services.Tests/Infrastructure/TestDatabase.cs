using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SIGMPOffline.Data;
using SIGMPOffline.Data.Database;
using SIGMPOffline.Data.Repositories;
using SIGMPOffline.Data.Repositories.Interfaces;
using SIGMPOffline.Services.Scoping;

namespace SIGMPOffline.Services.Tests.Infrastructure;

/// <summary>
/// Base SQLite en mémoire partagée pour les tests d'intégration.
/// Le schéma est créé via <c>EnsureCreated</c>.
/// </summary>
public sealed class TestDatabase : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;

    private TestDatabase(SqliteConnection connection, AppDbContext context, ProvinceAccessor provinceAccessor)
    {
        _connection = connection;
        Context = context;
        ProvinceAccessor = provinceAccessor;
        UnitOfWork = new UnitOfWork(context);

        // Les services de test sont exposés en singletons : les scopes créés par le
        // runner réutilisent donc le contexte et l'unité de travail de la base en mémoire.
        var services = new ServiceCollection();
        services.AddSingleton(context);
        services.AddSingleton(UnitOfWork);
        services.AddSingleton(provinceAccessor);
        _provider = services.BuildServiceProvider();

        ScopeRunner = new ScopeRunner(_provider.GetRequiredService<IServiceScopeFactory>());
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

    /// <summary>Exécuteur de scopes branché sur le contexte de test.</summary>
    public IScopeRunner ScopeRunner { get; }

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
        _provider.Dispose();
        await Context.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
