using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGMPOffline.Data.Converters;
using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Data.Configurations;

/// <summary>Configuration EF Core de <see cref="Avenant"/>.</summary>
public class AvenantConfiguration : IEntityTypeConfiguration<Avenant>
{
    public void Configure(EntityTypeBuilder<Avenant> builder)
    {
        builder.ToTable("Avenant");

        builder.HasKey(x => x.IdAvenant);

        builder.Property(x => x.IdAvenant).ValueGeneratedOnAdd();
        builder.Property(x => x.RefMarche).HasMaxLength(50).IsRequired();
        builder.Property(x => x.NumAvenant).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Motif).HasMaxLength(500);
        builder.Property(x => x.VariationMontant).HasConversion(DecimalConverters.Money);

        builder.HasIndex(x => x.RefMarche);
    }
}
