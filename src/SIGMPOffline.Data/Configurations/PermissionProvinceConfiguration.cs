using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Data.Configurations;

/// <summary>Configuration EF Core des autorisations compte/province.</summary>
public class PermissionProvinceConfiguration : IEntityTypeConfiguration<PermissionProvince>
{
    public void Configure(EntityTypeBuilder<PermissionProvince> builder)
    {
        builder.ToTable("PermissionProvince");
        builder.HasKey(x => new { x.IdUtilisateur, x.IdProvince });
        builder.Property(x => x.PeutModifier).IsRequired();

        builder.HasOne(x => x.Utilisateur)
            .WithMany(x => x.PermissionsProvinces)
            .HasForeignKey(x => x.IdUtilisateur)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Province)
            .WithMany(x => x.PermissionsUtilisateurs)
            .HasForeignKey(x => x.IdProvince)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
