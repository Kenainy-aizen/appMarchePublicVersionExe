using Microsoft.Extensions.DependencyInjection;

namespace SIGMPOffline.Services.Scoping;

/// <inheritdoc />
public sealed class ScopeRunner : IScopeRunner
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ScopeRunner(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    }

    /// <inheritdoc />
    public async Task<T> ExecuterAsync<T>(Func<IServiceProvider, Task<T>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        await using var scope = _scopeFactory.CreateAsyncScope();
        return await operation(scope.ServiceProvider);
    }

    /// <inheritdoc />
    public async Task ExecuterAsync(Func<IServiceProvider, Task> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        await using var scope = _scopeFactory.CreateAsyncScope();
        await operation(scope.ServiceProvider);
    }

    /// <inheritdoc />
    public T Executer<T>(Func<IServiceProvider, T> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        using var scope = _scopeFactory.CreateScope();
        return operation(scope.ServiceProvider);
    }
}
