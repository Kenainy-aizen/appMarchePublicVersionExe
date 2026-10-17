using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SIGMPOffline.Data;
using SIGMPOffline.Desktop.DependencyInjection;
using SIGMPOffline.Desktop.ViewModels;
using SIGMPOffline.Desktop.Views;

namespace SIGMPOffline.Desktop;

public partial class App : Application
{
    private ServiceProvider? _services;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        _services = AppServiceRegistration.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var rootViewModel = _services.GetRequiredService<RootViewModel>();

            desktop.MainWindow = new MainWindow
            {
                DataContext = rootViewModel
            };

            // Applique les migrations, puis aiguille vers l'assistant ou la connexion.
            desktop.MainWindow.Opened += async (_, _) =>
            {
                await AppliquerMigrationsAsync();
                await rootViewModel.DemarrerAsync();
            };

            desktop.Exit += (_, _) => _services?.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Applique les migrations EF Core en attente. Les données de démonstration
    /// sont créées avec la première province depuis l'assistant de configuration.
    /// </summary>
    private async Task AppliquerMigrationsAsync()
    {
        if (_services is null)
        {
            return;
        }

        try
        {
            await using var scope = _services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[SIGMP Offline] Échec de l'initialisation de la base : {ex.Message}");
        }
    }
}
