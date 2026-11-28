using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Data.Configurations;

/// <summary>Configuration EF Core du journal d'événements.</summary>
public class HistoriqueEvenementConfiguration : IEntityTypeConfiguration<HistoriqueEvenement>
{
    public void Configure(EntityTypeBuilder<HistoriqueEvenement> builder)
    {
        builder.ToTable("HistoriqueEvenement");
        builder.HasKey(x => x.IdHistorique);
        builder.Property(x => x.IdHistorique).ValueGeneratedOnAdd();
        builder.Property(x => x.DateEvenement).IsRequired();
        builder.Property(x => x.NomUtilisateur).HasMaxLength(200).IsRequired();
        builder.Property(x => x.NomProvince).HasMaxLength(150);
        builder.Property(x => x.Action).HasMaxLength(80).IsRequired();
        builder.Property(x => x.TypeEntite).HasMaxLength(120).IsRequired();
        builder.Property(x => x.ReferenceEntite).HasMaxLength(300);
        builder.Property(x => x.Detail).HasMaxLength(1000);

        // Pas de clé étrangère vers les comptes/provinces : les traces restent
        // consultables même après la suppression d'un compte.
        builder.HasIndex(x => x.DateEvenement);
        builder.HasIndex(x => x.IdUtilisateur);
        builder.HasIndex(x => x.IdProvince);
    }
}
