using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Data.Configurations;

/// <summary>Configuration EF Core de <see cref="Province"/>.</summary>
public class ProvinceConfiguration : IEntityTypeConfiguration<Province>
{
    public void Configure(EntityTypeBuilder<Province> builder)
    {
        builder.ToTable("Province");

        builder.HasKey(x => x.IdProvince);

        builder.Property(x => x.IdProvince).ValueGeneratedOnAdd();
        builder.Property(x => x.NomProvince).HasMaxLength(150).IsRequired();
        builder.Property(x => x.PasswordHash).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Salt).HasMaxLength(100).IsRequired();
        builder.Property(x => x.DateCreation).IsRequired();

        // Le nom de la province est unique.
        builder.HasIndex(x => x.NomProvince).IsUnique();
    }
}
