using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGMPOffline.Data.Converters;
using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Data.Configurations;

/// <summary>Configuration EF Core de <see cref="PpmDetail"/>.</summary>
public class PpmDetailConfiguration : IEntityTypeConfiguration<PpmDetail>
{
    public void Configure(EntityTypeBuilder<PpmDetail> builder)
    {
        builder.ToTable("PPMDetail");

        builder.HasKey(x => x.NumPPMDetail);

        builder.Property(x => x.NumPPMDetail).HasMaxLength(50);
        builder.Property(x => x.NumPPMEntete).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ObjetProjet).HasMaxLength(500).IsRequired();
        builder.Property(x => x.SourceFinancement).HasMaxLength(200);

        // Enums persistés en texte (lisibilité en base).
        builder.Property(x => x.NatureMarche)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.ModePassation)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        // decimal -> centimes (entier) pour conserver tri/agrégats SQL numériques.
        builder.Property(x => x.MontantEstimatif).HasConversion(DecimalConverters.Money);

        builder.HasIndex(x => x.NumPPMEntete);
    }
}
