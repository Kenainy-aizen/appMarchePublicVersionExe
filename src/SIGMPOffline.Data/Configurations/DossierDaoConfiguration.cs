using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGMPOffline.Data.Converters;
using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Data.Configurations;

/// <summary>Configuration EF Core de <see cref="DossierDao"/>.</summary>
public class DossierDaoConfiguration : IEntityTypeConfiguration<DossierDao>
{
    public void Configure(EntityTypeBuilder<DossierDao> builder)
    {
        builder.ToTable("DossierDAO");

        builder.HasKey(x => x.RefDossier);

        builder.Property(x => x.RefDossier).HasMaxLength(50);
        builder.Property(x => x.NumPPMDetail).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ObjetDossier).HasMaxLength(500).IsRequired();
        builder.Property(x => x.CoutCahierCharges).HasConversion(DecimalConverters.Money);
        builder.Property(x => x.MontantCaution).HasConversion(DecimalConverters.Money);

        builder.HasIndex(x => x.NumPPMDetail);
        builder.HasIndex(x => x.IdProvince);

        // Rattachement (optionnel) à une province : isolation multi-provinces.
        builder.HasOne(x => x.Province)
            .WithMany()
            .HasForeignKey(x => x.IdProvince)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.PpmDetail)
            .WithMany(x => x.Dossiers)
            .HasForeignKey(x => x.NumPPMDetail)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Marche)
            .WithOne(x => x.Dossier)
            .HasForeignKey<Marche>(x => x.RefDossier)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
