using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SIGMPOffline.Data.Database;
using SIGMPOffline.Data.Repositories;
using SIGMPOffline.Data.Repositories.Impl;
using SIGMPOffline.Data.Repositories.Interfaces;
using SIGMPOffline.Domain.Common;

namespace SIGMPOffline.Data;

/// <summary>Enregistrement des services d'accès aux données dans le conteneur DI.</summary>
public static class DataServiceCollectionExtensions
{
    /// <summary>
    /// Ajoute le <see cref="AppDbContext"/> SQLite au conteneur d'injection de dépendances.
    /// </summary>
    /// <param name="services">Collection de services.</param>
    /// <param name="connectionString">
    /// Chaîne de connexion SQLite. Si <c>null</c>, le chemin par défaut de l'utilisateur est utilisé.
    /// </param>
    public static IServiceCollection AddSigmpData(
        this IServiceCollection services,
        string? connectionString = null)
    {
        var cs = string.IsNullOrWhiteSpace(connectionString)
            ? AppPaths.DefaultConnectionString
            : connectionString;

        services.AddDbContext<AppDbContext>(options => options.UseSqlite(cs));

        return services;
    }

    /// <summary>Enregistre les repositories, l'unité de travail et le contexte de province.</summary>
    public static IServiceCollection AddSigmpRepositories(this IServiceCollection services)
    {
        // La province active est un état partagé par toute l'application (singleton).
        services.AddSingleton<IProvinceAccessor, ProvinceAccessor>();

        services.AddScoped<IPpmRepository, PpmRepository>();
        services.AddScoped<IDossierDaoRepository, DossierDaoRepository>();
        services.AddScoped<IMarcheRepository, MarcheRepository>();
        services.AddScoped<IProvinceRepository, ProvinceRepository>();
        services.AddScoped<IConfigurationAppRepository, ConfigurationAppRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    /// <summary>Enregistre le contexte EF Core et toute la couche d'accès aux données.</summary>
    public static IServiceCollection AddSigmpDataAccess(
        this IServiceCollection services,
        string? connectionString = null)
    {
        services.AddSigmpData(connectionString);
        services.AddSigmpRepositories();
        return services;
    }
}
