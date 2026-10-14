using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SIGMPOffline.Data;
using SIGMPOffline.Data.Database;
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
            var mainViewModel = _services.GetRequiredService<MainWindowViewModel>();

            desktop.MainWindow = new MainWindow
            {
                DataContext = mainViewModel
            };

            // Initialise la base (migrations + données de démo) puis charge la 1re page.
            desktop.MainWindow.Opened += async (_, _) =>
            {
                await InitialiserBaseAsync();
                await mainViewModel.InitialiserAsync();
            };

            desktop.Exit += (_, _) => _services?.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task InitialiserBaseAsync()
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
            await DbSeeder.SeedAsync(context);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[SIGMP Offline] Échec de l'initialisation de la base : {ex.Message}");
        }
    }
}
