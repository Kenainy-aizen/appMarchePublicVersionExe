using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SIGMPOffline.Data;
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

    private TestDatabase(SqliteConnection connection, AppDbContext context)
    {
        _connection = connection;
        Context = context;
        UnitOfWork = new UnitOfWork(context);
    }

    /// <summary>Contexte EF Core de test.</summary>
    public AppDbContext Context { get; }

    /// <summary>Unité de travail de test.</summary>
    public IUnitOfWork UnitOfWork { get; }

    /// <summary>Crée une base en mémoire prête à l'emploi.</summary>
    public static async Task<TestDatabase> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        return new TestDatabase(connection, context);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
