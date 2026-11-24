using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SIGMPOffline.Data;
using SIGMPOffline.Desktop.Services;
using SIGMPOffline.Desktop.ViewModels;
using SIGMPOffline.Services;

namespace SIGMPOffline.Desktop.DependencyInjection;

/// <summary>Construit le conteneur d'injection de dépendances de l'application.</summary>
public static class AppServiceRegistration
{
    /// <summary>Configure et construit le <see cref="ServiceProvider"/>.</summary>
    /// <param name="connectionString">
    /// Chaîne de connexion SQLite optionnelle (utile pour les tests et les outils).
    /// Si <c>null</c>, le chemin par défaut de l'utilisateur est utilisé.
    /// </param>
    public static ServiceProvider BuildServiceProvider(string? connectionString = null)
    {
        var services = new ServiceCollection();

        services.AddLogging(builder => builder.AddConsole());

        // Accès aux données (DbContext + repositories + UnitOfWork)
        services.AddSigmpDataAccess(connectionString);

        // Services métier (les 6 modules)
        services.AddSigmpServices();

        // Services d'IHM
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IFilePickerService, FilePickerService>();
        services.AddSingleton<ModeLectureService>();

        // ViewModels
        services.AddSingleton<RootViewModel>();
        services.AddSingleton<SetupViewModel>();
        services.AddSingleton<LoginViewModel>();
        services.AddSingleton<ShellViewModel>();
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<PpmViewModel>();
        services.AddSingleton<DossierDaoViewModel>();
        services.AddSingleton<MarcheViewModel>();
        services.AddSingleton<SuiviViewModel>();
        services.AddSingleton<BackupViewModel>();

        return services.BuildServiceProvider();
    }
}
