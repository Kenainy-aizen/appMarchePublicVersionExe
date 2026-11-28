using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SIGMPOffline.Data.Database;
using SIGMPOffline.Domain.Common;
using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Data;

/// <summary>
/// Contexte Entity Framework Core de l'application SIGMP Offline (SQLite).
/// Les entités sont configurées via des <c>IEntityTypeConfiguration</c> séparés,
/// découverts automatiquement dans l'assembly.
/// </summary>
/// <remarks>
/// Une <b>isolation multi-provinces</b> est assurée par des filtres de requête globaux :
/// seules les données de la province active (fournie par <see cref="IProvinceAccessor"/>)
/// sont visibles. Les nouvelles entités sont automatiquement rattachées à cette province.
/// </remarks>
public class AppDbContext : DbContext
{
    private readonly IProvinceAccessor _provinceAccessor;
    private readonly IUtilisateurAccessor? _utilisateurAccessor;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        IProvinceAccessor provinceAccessor,
        IUtilisateurAccessor? utilisateurAccessor = null)
        : base(options)
    {
        _provinceAccessor = provinceAccessor ?? throw new ArgumentNullException(nameof(provinceAccessor));
        _utilisateurAccessor = utilisateurAccessor;
    }

    /// <summary>En-têtes de PPM.</summary>
    public DbSet<PpmEntete> PpmEntetes => Set<PpmEntete>();

    /// <summary>Lignes de détail des PPM.</summary>
    public DbSet<PpmDetail> PpmDetails => Set<PpmDetail>();

    /// <summary>Dossiers d'appel d'offres.</summary>
    public DbSet<DossierDao> DossiersDao => Set<DossierDao>();

    /// <summary>Acheteurs des dossiers DAO.</summary>
    public DbSet<AcheteurDao> AcheteursDao => Set<AcheteurDao>();

    /// <summary>Candidats / soumissionnaires.</summary>
    public DbSet<Candidat> Candidats => Set<Candidat>();

    /// <summary>Marchés.</summary>
    public DbSet<Marche> Marches => Set<Marche>();

    /// <summary>Éléments / articles des marchés.</summary>
    public DbSet<ElementMarche> ElementsMarche => Set<ElementMarche>();

    /// <summary>Suivis d'exécution.</summary>
    public DbSet<SuiviExecution> SuivisExecution => Set<SuiviExecution>();

    /// <summary>Avenants.</summary>
    public DbSet<Avenant> Avenants => Set<Avenant>();

    /// <summary>Provinces gérées par l'application.</summary>
    public DbSet<Province> Provinces => Set<Province>();

    /// <summary>Comptes utilisateurs et rôles.</summary>
    public DbSet<Utilisateur> Utilisateurs => Set<Utilisateur>();

    /// <summary>Autorisations des comptes sur les provinces.</summary>
    public DbSet<PermissionProvince> PermissionsProvinces => Set<PermissionProvince>();

    /// <summary>Historique des opérations et événements d'authentification.</summary>
    public DbSet<HistoriqueEvenement> HistoriqueEvenements => Set<HistoriqueEvenement>();

    /// <summary>Configuration globale de l'application (une seule ligne).</summary>
    public DbSet<ConfigurationApp> Configurations => Set<ConfigurationApp>();

    /// <summary>
    /// Province active actuellement sélectionnée, ou <c>null</c> avant connexion.
    /// Évaluée à chaque requête par les filtres globaux ci-dessous.
    /// </summary>
    public int? IdProvinceActive => _provinceAccessor.IdProvince;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Charge toutes les IEntityTypeConfiguration<T> de l'assembly.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Isolation multi-provinces : aucune province active ⇒ pas de filtre
        // (utilisé à l'initialisation) ; sinon, seules les données de la province active sont visibles.
        //
        // Les agrégats racines portent la province ; leurs entités filles la reprennent
        // via leur parent (filtre cohérent exigé par EF Core, et isolation en profondeur).
        modelBuilder.Entity<PpmEntete>()
            .HasQueryFilter(e => IdProvinceActive == null || e.IdProvince == IdProvinceActive);
        modelBuilder.Entity<DossierDao>()
            .HasQueryFilter(e => IdProvinceActive == null || e.IdProvince == IdProvinceActive);
        modelBuilder.Entity<Marche>()
            .HasQueryFilter(e => IdProvinceActive == null || e.IdProvince == IdProvinceActive);

        modelBuilder.Entity<PpmDetail>()
            .HasQueryFilter(e => IdProvinceActive == null || e.Entete!.IdProvince == IdProvinceActive);
        modelBuilder.Entity<AcheteurDao>()
            .HasQueryFilter(e => IdProvinceActive == null || e.Dossier!.IdProvince == IdProvinceActive);
        modelBuilder.Entity<Candidat>()
            .HasQueryFilter(e => IdProvinceActive == null || e.Dossier!.IdProvince == IdProvinceActive);
        modelBuilder.Entity<ElementMarche>()
            .HasQueryFilter(e => IdProvinceActive == null || e.Marche!.IdProvince == IdProvinceActive);
        modelBuilder.Entity<Avenant>()
            .HasQueryFilter(e => IdProvinceActive == null || e.Marche!.IdProvince == IdProvinceActive);
        modelBuilder.Entity<SuiviExecution>()
            .HasQueryFilter(e => IdProvinceActive == null || e.Marche!.IdProvince == IdProvinceActive);
    }

    /// <inheritdoc />
    public override int SaveChanges()
        => SaveChanges(acceptAllChangesOnSuccess: true);

    /// <inheritdoc />
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EstampillerProvince();
        var changements = CapturerChangementsJournalisables();
        if (changements.Count == 0)
        {
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        var transactionPropre = Database.CurrentTransaction is null
            ? Database.BeginTransaction()
            : null;

        try
        {
            var nombreEcritures = base.SaveChanges(acceptAllChangesOnSuccess: false);
            foreach (var changement in changements)
            {
                var evenement = CreerEvenement(changement);
                Database.ExecuteSqlInterpolated($"""
                    INSERT INTO "HistoriqueEvenement"
                        ("DateEvenement", "IdUtilisateur", "NomUtilisateur", "IdProvince", "NomProvince",
                         "Action", "TypeEntite", "ReferenceEntite", "Detail")
                    VALUES ({evenement.DateEvenement}, {evenement.IdUtilisateur}, {evenement.NomUtilisateur},
                            {evenement.IdProvince}, {evenement.NomProvince}, {evenement.Action},
                            {evenement.TypeEntite}, {evenement.ReferenceEntite}, {evenement.Detail});
                    """);
            }

            if (acceptAllChangesOnSuccess)
            {
                ChangeTracker.AcceptAllChanges();
            }

            transactionPropre?.Commit();
            return nombreEcritures + changements.Count;
        }
        catch
        {
            transactionPropre?.Rollback();
            throw;
        }
        finally
        {
            transactionPropre?.Dispose();
        }
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(acceptAllChangesOnSuccess: true, cancellationToken);

    /// <inheritdoc />
    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        EstampillerProvince();
        var changements = CapturerChangementsJournalisables();
        if (changements.Count == 0)
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        var transactionPropre = Database.CurrentTransaction is null
            ? await Database.BeginTransactionAsync(cancellationToken)
            : null;

        try
        {
            var nombreEcritures = await base.SaveChangesAsync(
                acceptAllChangesOnSuccess: false, cancellationToken);
            foreach (var changement in changements)
            {
                var evenement = CreerEvenement(changement);
                await Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO "HistoriqueEvenement"
                        ("DateEvenement", "IdUtilisateur", "NomUtilisateur", "IdProvince", "NomProvince",
                         "Action", "TypeEntite", "ReferenceEntite", "Detail")
                    VALUES ({evenement.DateEvenement}, {evenement.IdUtilisateur}, {evenement.NomUtilisateur},
                            {evenement.IdProvince}, {evenement.NomProvince}, {evenement.Action},
                            {evenement.TypeEntite}, {evenement.ReferenceEntite}, {evenement.Detail});
                    """, cancellationToken);
            }

            if (acceptAllChangesOnSuccess)
            {
                ChangeTracker.AcceptAllChanges();
            }

            if (transactionPropre is not null)
            {
                await transactionPropre.CommitAsync(cancellationToken);
            }

            return nombreEcritures + changements.Count;
        }
        catch
        {
            if (transactionPropre is not null)
            {
                await transactionPropre.RollbackAsync(cancellationToken);
            }

            throw;
        }
        finally
        {
            if (transactionPropre is not null)
            {
                await transactionPropre.DisposeAsync();
            }
        }
    }

    private List<ChangementJournalisable> CapturerChangementsJournalisables()
    {
        if (_utilisateurAccessor?.IdUtilisateur is null)
        {
            return new List<ChangementJournalisable>();
        }

        ChangeTracker.DetectChanges();
        return ChangeTracker.Entries()
            .Where(entree => entree.Entity is not HistoriqueEvenement
                && entree.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(entree => new ChangementJournalisable(
                entree,
                entree.State switch
                {
                    EntityState.Added => "Création",
                    EntityState.Modified => "Modification",
                    EntityState.Deleted => "Suppression",
                    _ => string.Empty
                }))
            .ToList();
    }

    private HistoriqueEvenement CreerEvenement(ChangementJournalisable changement)
    {
        var entree = changement.Entree;
        var cles = entree.Metadata.FindPrimaryKey()?.Properties
            .Select(propriete => $"{propriete.Name}={entree.Property(propriete.Name).CurrentValue ?? "?"}")
            ?? Enumerable.Empty<string>();

        var idProvince = LireIdProvince(entree) ?? _provinceAccessor.IdProvince;
        var champsModifies = changement.Action == "Modification"
            ? string.Join(", ", entree.Properties
                .Where(propriete => propriete.IsModified
                    && propriete.Metadata.Name is not ("PasswordHash" or "Salt"))
                .Select(propriete => propriete.Metadata.Name))
            : null;
        var identifiantsModifies = changement.Action == "Modification"
            && entree.Entity is Utilisateur
            && entree.Properties.Any(propriete => propriete.IsModified
                && propriete.Metadata.Name is "PasswordHash" or "Salt");

        return new HistoriqueEvenement
        {
            DateEvenement = DateTime.Now,
            IdUtilisateur = _utilisateurAccessor!.IdUtilisateur,
            NomUtilisateur = Tronquer(_utilisateurAccessor.NomUtilisateur, 200) ?? "Utilisateur inconnu",
            IdProvince = idProvince,
            NomProvince = Tronquer(LireNomProvince(entree) ?? _utilisateurAccessor.NomProvince, 150),
            Action = Tronquer(changement.Action, 80) ?? string.Empty,
            TypeEntite = Tronquer(entree.Metadata.ClrType.Name, 120) ?? string.Empty,
            ReferenceEntite = Tronquer(string.Join(" ; ", cles), 300),
            Detail = Tronquer(identifiantsModifies
                ? "Mot de passe modifié (valeur masquée)"
                : string.IsNullOrWhiteSpace(champsModifies) ? null : $"Champs : {champsModifies}", 1000)
        };
    }

    /// <summary>Évite qu'une valeur inattendue ne déborde la colonne du journal.</summary>
    internal static string? Tronquer(string? valeur, int longueur)
        => valeur is null ? null : valeur.Length <= longueur ? valeur : valeur[..longueur];

    private static int? LireIdProvince(EntityEntry entree)
    {
        var propriete = entree.Metadata.FindProperty(nameof(Province.IdProvince))
            ?? entree.Metadata.FindProperty(nameof(PermissionProvince.IdProvince));
        return propriete?.Name is { } nom && entree.Property(nom).CurrentValue is int valeur
            ? valeur
            : null;
    }

    private static string? LireNomProvince(EntityEntry entree)
        => entree.Entity switch
        {
            Province province => province.NomProvince,
            PermissionProvince { Province: not null } permission => permission.Province.NomProvince,
            _ => null
        };

    private sealed record ChangementJournalisable(EntityEntry Entree, string Action);

    /// <summary>Rattache automatiquement les nouvelles entités à la province active.</summary>
    private void EstampillerProvince()
    {
        if (IdProvinceActive is not int idProvince)
        {
            return;
        }

        foreach (var entry in ChangeTracker.Entries<IProvinceScoped>())
        {
            if (entry.State == EntityState.Added && entry.Entity.IdProvince is null)
            {
                entry.Entity.IdProvince = idProvince;
            }
        }
    }
}
