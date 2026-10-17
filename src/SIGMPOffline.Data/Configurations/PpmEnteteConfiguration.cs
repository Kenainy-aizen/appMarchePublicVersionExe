using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Data.Configurations;

/// <summary>Configuration EF Core de <see cref="PpmEntete"/>.</summary>
public class PpmEnteteConfiguration : IEntityTypeConfiguration<PpmEntete>
{
    public void Configure(EntityTypeBuilder<PpmEntete> builder)
    {
        builder.ToTable("PPMEntete");

        builder.HasKey(x => x.NumPPM);

        builder.Property(x => x.NumPPM).HasMaxLength(50).IsRequired();
        builder.Property(x => x.AutoriteContractante).HasMaxLength(200).IsRequired();
        builder.Property(x => x.AnneeExercice).IsRequired();
        builder.Property(x => x.DateElaboration).IsRequired();
        builder.Property(x => x.NumDerniereMAJ).HasMaxLength(50);

        builder.HasIndex(x => x.AnneeExercice);
        builder.HasIndex(x => x.IdProvince);

        // Rattachement (optionnel) à une province : isolation multi-provinces.
        builder.HasOne(x => x.Province)
            .WithMany()
            .HasForeignKey(x => x.IdProvince)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Details)
            .WithOne(x => x.Entete)
            .HasForeignKey(x => x.NumPPMEntete)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
