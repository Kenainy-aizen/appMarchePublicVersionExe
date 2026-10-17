using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Data.Configurations;

/// <summary>Configuration EF Core de <see cref="AcheteurDao"/>.</summary>
public class AcheteurDaoConfiguration : IEntityTypeConfiguration<AcheteurDao>
{
    public void Configure(EntityTypeBuilder<AcheteurDao> builder)
    {
        builder.ToTable("AcheteurDAO");

        builder.HasKey(x => x.IdAcheteur);

        builder.Property(x => x.IdAcheteur).ValueGeneratedOnAdd();
        builder.Property(x => x.RefDossier).HasMaxLength(50).IsRequired();
        builder.Property(x => x.NumStat).HasMaxLength(50).IsRequired();
        builder.Property(x => x.NumNIF).HasMaxLength(50).IsRequired();
        builder.Property(x => x.RaisonSociale).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Adresse).HasMaxLength(300);
        builder.Property(x => x.DateAchat).IsRequired();

        builder.HasIndex(x => x.RefDossier);
        // NumStat sert de clé alternée : le titulaire du marché (Marche.NumStatTitulaire)
        // y fait référence. Elle doit donc être unique.
        builder.HasAlternateKey(x => x.NumStat);

        builder.HasOne(x => x.Dossier)
            .WithMany(x => x.Acheteurs)
            .HasForeignKey(x => x.RefDossier)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
