using Microsoft.Extensions.DependencyInjection;

namespace SIGMPOffline.Services.Scoping;

/// <summary>
/// Raccourcis typés pour résoudre un service Scoped dans le scope d'un
/// <see cref="IScopeRunner"/> sans manipuler directement le fournisseur.
/// </summary>
public static class ScopeRunnerExtensions
{
    /// <summary>Exécute une opération asynchrone sur un service Scoped résolu pour l'occasion.</summary>
    public static Task ExecuterAsync<TService>(
        this IScopeRunner runner,
        Func<TService, Task> operation)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(operation);

        return runner.ExecuterAsync(
            services => operation(services.GetRequiredService<TService>()));
    }

    /// <summary>Exécute une opération asynchrone sur un service Scoped et retourne son résultat.</summary>
    public static Task<TResult> ExecuterAsync<TService, TResult>(
        this IScopeRunner runner,
        Func<TService, Task<TResult>> operation)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(operation);

        return runner.ExecuterAsync(
            services => operation(services.GetRequiredService<TService>()));
    }

    /// <summary>Exécute une opération synchrone sur un service Scoped et retourne son résultat.</summary>
    public static TResult Executer<TService, TResult>(
        this IScopeRunner runner,
        Func<TService, TResult> operation)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(operation);

        return runner.Executer(
            services => operation(services.GetRequiredService<TService>()));
    }
}
