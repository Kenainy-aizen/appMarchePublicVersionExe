using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SIGMPOffline.Data.Repositories.Impl;
using SIGMPOffline.Data.Repositories.Interfaces;

namespace SIGMPOffline.Data.Repositories;

/// <inheritdoc />
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));

        Ppm = new PpmRepository(_context);
        Dossiers = new DossierDaoRepository(_context);
        Marches = new MarcheRepository(_context);
        Provinces = new ProvinceRepository(_context);
        Configuration = new ConfigurationAppRepository(_context);
    }

    /// <inheritdoc />
    public IPpmRepository Ppm { get; }

    /// <inheritdoc />
    public IDossierDaoRepository Dossiers { get; }

    /// <inheritdoc />
    public IMarcheRepository Marches { get; }

    /// <inheritdoc />
    public IProvinceRepository Provinces { get; }

    /// <inheritdoc />
    public IConfigurationAppRepository Configuration { get; }

    /// <inheritdoc />
    public void DetacherTout() => _context.ChangeTracker.Clear();

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            // Une écriture refusée (clé dupliquée, contrainte…) laisse sinon les entités
            // fautives suivies en état Added : la moindre sauvegarde suivante échouerait
            // à nouveau. On détache uniquement les entités dont la persistance a échoué,
            // puis on relance l'exception d'origine sans la masquer.
            DetacherEntreesEnEchec(ex);
            throw;
        }
    }

    /// <summary>
    /// Détache les entités dont la persistance a échoué afin que le contexte reste
    /// utilisable pour les opérations suivantes.
    /// </summary>
    private void DetacherEntreesEnEchec(DbUpdateException exception)
    {
        var entrees = exception.Entries.Count > 0
            ? exception.Entries.Select(entree => entree)
            : _context.ChangeTracker.Entries().Where(entree => entree.State == EntityState.Added);

        foreach (var entree in entrees.ToList())
        {
            if (entree.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            {
                entree.State = EntityState.Detached;
            }
        }
    }

    /// <inheritdoc />
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            return;
        }

        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            return;
        }

        await SaveChangesAsync(cancellationToken);
        await _transaction.CommitAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    /// <inheritdoc />
    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            return;
        }

        await _transaction.RollbackAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }

        await _context.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
