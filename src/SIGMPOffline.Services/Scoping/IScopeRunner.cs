namespace SIGMPOffline.Services.Scoping;

/// <summary>
/// Exécute une opération dans un scope d'injection de dépendances dédié.
/// </summary>
/// <remarks>
/// Les ViewModels et <see cref="Abstractions.IAuthService"/> sont des singletons : ils ne
/// doivent pas capturer de service Scoped (<c>AppDbContext</c>, <c>IUnitOfWork</c>,
/// services métier). Chaque opération résout ses dépendances dans un scope court,
/// libéré à la fin de l'appel, ce qui garantit qu'un contexte EF Core n'est jamais
/// partagé entre deux opérations.
/// </remarks>
public interface IScopeRunner
{
    /// <summary>Exécute une opération asynchrone dans un scope et retourne son résultat.</summary>
    Task<T> ExecuterAsync<T>(Func<IServiceProvider, Task<T>> operation);

    /// <summary>Exécute une opération asynchrone dans un scope.</summary>
    Task ExecuterAsync(Func<IServiceProvider, Task> operation);

    /// <summary>
    /// Exécute une opération synchrone dans un scope, pour les valeurs qui ne
    /// nécessitent pas d'appel asynchrone (chemin de la base, par exemple).
    /// </summary>
    T Executer<T>(Func<IServiceProvider, T> operation);
}
