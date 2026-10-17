using Microsoft.Extensions.DependencyInjection;
using SIGMPOffline.Services.Abstractions;
using SIGMPOffline.Services.Auth;
using SIGMPOffline.Services.Backup;
using SIGMPOffline.Services.Dashboard;
using SIGMPOffline.Services.Dao;
using SIGMPOffline.Services.Marches;
using SIGMPOffline.Services.Ppm;
using SIGMPOffline.Services.Suivi;

namespace SIGMPOffline.Services;

/// <summary>Enregistrement des services métier dans le conteneur DI.</summary>
public static class ServicesServiceCollectionExtensions
{
    /// <summary>Enregistre les services des 6 modules fonctionnels.</summary>
    public static IServiceCollection AddSigmpServices(this IServiceCollection services)
    {
        services.AddScoped<IPpmService, PpmService>();
        services.AddScoped<IDossierDaoService, DossierDaoService>();
        services.AddScoped<IMarcheService, MarcheService>();
        services.AddScoped<ISuiviExecutionService, SuiviExecutionService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IBackupService, BackupService>();

        // Authentification / multi-provinces : état de session partagé (singleton).
        services.AddSingleton<IAuthService, AuthService>();

        return services;
    }
}
