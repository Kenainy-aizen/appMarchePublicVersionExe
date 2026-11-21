using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGMPOffline.Data.Converters;
using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Data.Configurations;

/// <summary>Configuration EF Core de <see cref="PpmBeneficiaire"/>.</summary>
public class PpmBeneficiaireConfiguration : IEntityTypeConfiguration<PpmBeneficiaire>
{
    public void Configure(EntityTypeBuilder<PpmBeneficiaire> builder)
    {
        builder.ToTable("PPMBeneficiaire");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.NumPPMDetail).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Service).HasMaxLength(200).IsRequired();

        // Même représentation que MontantEstimatif (centimes en entier).
        builder.Property(x => x.Montant).HasConversion(DecimalConverters.Money);

        // Supprimer une ligne du plan supprime ses services bénéficiaires.
        builder.HasOne(x => x.Detail)
            .WithMany(x => x.Beneficiaires)
            .HasForeignKey(x => x.NumPPMDetail)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.NumPPMDetail);
    }
}
