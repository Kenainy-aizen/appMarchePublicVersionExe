using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGMPOffline.Data.Converters;
using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Data.Configurations;

/// <summary>Configuration EF Core de <see cref="ElementMarche"/>.</summary>
public class ElementMarcheConfiguration : IEntityTypeConfiguration<ElementMarche>
{
    public void Configure(EntityTypeBuilder<ElementMarche> builder)
    {
        builder.ToTable("ElementMarche");

        builder.HasKey(x => x.CodeElement);

        builder.Property(x => x.CodeElement).HasMaxLength(50);
        builder.Property(x => x.RefMarche).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Libelle).HasMaxLength(500).IsRequired();
        builder.Property(x => x.PrixUnitaire).HasConversion(DecimalConverters.Money);
        builder.Property(x => x.Quantite).HasConversion(DecimalConverters.Quantity);
        builder.Property(x => x.Unite).HasMaxLength(30).IsRequired();

        // Propriété calculée, non persistée.
        builder.Ignore(x => x.MontantLigne);

        builder.HasIndex(x => x.RefMarche);
    }
}
