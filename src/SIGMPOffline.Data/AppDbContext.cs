using Microsoft.EntityFrameworkCore;
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

    public AppDbContext(DbContextOptions<AppDbContext> options, IProvinceAccessor provinceAccessor)
        : base(options)
    {
        _provinceAccessor = provinceAccessor ?? throw new ArgumentNullException(nameof(provinceAccessor));
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
    {
        EstampillerProvince();
        return base.SaveChanges();
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        EstampillerProvince();
        return base.SaveChangesAsync(cancellationToken);
    }

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
