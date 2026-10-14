using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGMPOffline.Data.Converters;
using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Data.Configurations;

/// <summary>Configuration EF Core de <see cref="Candidat"/>.</summary>
public class CandidatConfiguration : IEntityTypeConfiguration<Candidat>
{
    public void Configure(EntityTypeBuilder<Candidat> builder)
    {
        builder.ToTable("Candidat");

        builder.HasKey(x => x.IdCandidat);

        builder.Property(x => x.IdCandidat).ValueGeneratedOnAdd();
        builder.Property(x => x.RefDossier).HasMaxLength(50).IsRequired();
        builder.Property(x => x.NumStat).HasMaxLength(50).IsRequired();
        builder.Property(x => x.NumNIF).HasMaxLength(50).IsRequired();
        builder.Property(x => x.NomEntreprise).HasMaxLength(200).IsRequired();
        builder.Property(x => x.MontantOffre).HasConversion(DecimalConverters.Money);

        builder.HasIndex(x => x.RefDossier);
        builder.HasIndex(x => x.EstRetenu);

        builder.HasOne(x => x.Dossier)
            .WithMany(x => x.Candidats)
            .HasForeignKey(x => x.RefDossier)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
