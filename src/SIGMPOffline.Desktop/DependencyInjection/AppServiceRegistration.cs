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
    public static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddLogging(builder => builder.AddConsole());

        // Accès aux données (DbContext + repositories + UnitOfWork)
        services.AddSigmpDataAccess();

        // Services métier (les 6 modules)
        services.AddSigmpServices();

        // Services d'IHM
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IFilePickerService, FilePickerService>();

        // ViewModels
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<PpmViewModel>();
        services.AddSingleton<DossierDaoViewModel>();
        services.AddSingleton<MarcheViewModel>();
        services.AddSingleton<SuiviViewModel>();
        services.AddSingleton<BackupViewModel>();

        return services.BuildServiceProvider();
    }
}
