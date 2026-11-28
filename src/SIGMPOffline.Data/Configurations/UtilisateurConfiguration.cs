using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Data.Configurations;

/// <summary>Configuration EF Core des comptes utilisateur.</summary>
public class UtilisateurConfiguration : IEntityTypeConfiguration<Utilisateur>
{
    public void Configure(EntityTypeBuilder<Utilisateur> builder)
    {
        builder.ToTable("Utilisateur");
        builder.HasKey(x => x.IdUtilisateur);

        builder.Property(x => x.IdUtilisateur).ValueGeneratedOnAdd();
        builder.Property(x => x.NomUtilisateur)
            .UseCollation("NOCASE")
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(x => x.PasswordHash).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Salt).HasMaxLength(100).IsRequired();
        builder.Property(x => x.EstAdministrateur).IsRequired();
        builder.Property(x => x.EstActif).IsRequired();
        builder.Property(x => x.DateCreation).IsRequired();

        builder.HasIndex(x => x.NomUtilisateur).IsUnique();
    }
}
