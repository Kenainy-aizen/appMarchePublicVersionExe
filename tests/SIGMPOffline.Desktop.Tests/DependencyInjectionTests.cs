using Microsoft.Extensions.DependencyInjection;
using SIGMPOffline.Desktop.DependencyInjection;
using SIGMPOffline.Desktop.ViewModels;
using SIGMPOffline.Services.Abstractions;

namespace SIGMPOffline.Desktop.Tests;

/// <summary>
/// Vérifie que le conteneur réel respecte les durées de vie : aucun singleton
/// (ViewModel, AuthService) ne capture de service Scoped (DbContext, UnitOfWork).
/// </summary>
public class DependencyInjectionTests
{
    [Fact]
    public void Le_conteneur_valide_les_durees_de_vie_et_resout_les_singletons()
    {
        var chemin = Path.Combine(
            Path.GetTempPath(),
            $"sigmp-di-{Guid.NewGuid():N}.db");

        using var provider = AppServiceRegistration.BuildServiceProvider($"Data Source={chemin}");

        // BuildServiceProvider active ValidateScopes et ValidateOnBuild : la
        // construction échouerait si un singleton dépendait d'un service Scoped.
        Assert.NotNull(provider.GetRequiredService<IAuthService>());
        Assert.NotNull(provider.GetRequiredService<ShellViewModel>());
        Assert.NotNull(provider.GetRequiredService<DashboardViewModel>());
    }
}
