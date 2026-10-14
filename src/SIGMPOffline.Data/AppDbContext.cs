using Microsoft.EntityFrameworkCore;
using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Data;

/// <summary>
/// Contexte Entity Framework Core de l'application SIGMP Offline (SQLite).
/// Les entités sont configurées via des <c>IEntityTypeConfiguration</c> séparés,
/// découverts automatiquement dans l'assembly.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Charge toutes les IEntityTypeConfiguration<T> de l'assembly.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
