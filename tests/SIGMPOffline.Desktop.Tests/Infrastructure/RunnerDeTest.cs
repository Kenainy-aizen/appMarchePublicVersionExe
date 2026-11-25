using Microsoft.Extensions.DependencyInjection;
using SIGMPOffline.Services.Scoping;

namespace SIGMPOffline.Desktop.Tests.Infrastructure;

/// <summary>
/// Construit un <see cref="IScopeRunner"/> qui résout un unique service de test.
/// Le service est enregistré en singleton dans un conteneur jetable : les scopes
/// créés par le runner renvoient donc toujours la même instance factice.
/// </summary>
internal static class RunnerDeTest
{
    public static IScopeRunner Creer<TService>(TService service)
        where TService : class
    {
        var services = new ServiceCollection();
        services.AddSingleton(service);

        var provider = services.BuildServiceProvider();
        return new ScopeRunner(provider.GetRequiredService<IServiceScopeFactory>());
    }
}
