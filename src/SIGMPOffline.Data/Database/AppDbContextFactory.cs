using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SIGMPOffline.Data.Database;

/// <summary>
/// Fabrique de contexte utilisée uniquement par les outils EF Core
/// (<c>dotnet ef migrations add</c> / <c>database update</c>).
/// Permet de générer les migrations sans dépendre du projet Desktop.
/// Variable d'environnement <c>SIGMP_CONNECTION</c> pour surcharger la connexion.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("SIGMP_CONNECTION")
            ?? AppPaths.DefaultConnectionString;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connectionString)
            .Options;

        return new AppDbContext(options, new ProvinceAccessor());
    }
}
