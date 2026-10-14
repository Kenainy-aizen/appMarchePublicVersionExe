using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGMPOffline.Data.Converters;
using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Data.Configurations;

/// <summary>Configuration EF Core de <see cref="Marche"/>.</summary>
public class MarcheConfiguration : IEntityTypeConfiguration<Marche>
{
    public void Configure(EntityTypeBuilder<Marche> builder)
    {
        builder.ToTable("Marche");

        builder.HasKey(x => x.RefMarche);

        builder.Property(x => x.RefMarche).HasMaxLength(50);
        builder.Property(x => x.RefDossier).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ObjetMarche).HasMaxLength(500).IsRequired();
        builder.Property(x => x.MontantHT).HasConversion(DecimalConverters.Money);
        builder.Property(x => x.MontantTTC).HasConversion(DecimalConverters.Money);
        builder.Property(x => x.DelaiExecutionJours).IsRequired();

        builder.Property(x => x.StatutExecution)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.HasIndex(x => x.RefDossier);
        builder.HasIndex(x => x.IdCandidatTitulaire);
        builder.HasIndex(x => x.StatutExecution);

        // Titulaire : candidat retenu (relation optionnelle).
        builder.HasOne(x => x.Titulaire)
            .WithMany()
            .HasForeignKey(x => x.IdCandidatTitulaire)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Elements)
            .WithOne(x => x.Marche)
            .HasForeignKey(x => x.RefMarche)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Avenants)
            .WithOne(x => x.Marche)
            .HasForeignKey(x => x.RefMarche)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Suivi)
            .WithOne(x => x.Marche)
            .HasForeignKey<SuiviExecution>(x => x.RefMarche)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
