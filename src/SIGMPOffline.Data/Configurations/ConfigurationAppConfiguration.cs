using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Data.Configurations;

/// <summary>Configuration EF Core de <see cref="ConfigurationApp"/>.</summary>
public class ConfigurationAppConfiguration : IEntityTypeConfiguration<ConfigurationApp>
{
    public void Configure(EntityTypeBuilder<ConfigurationApp> builder)
    {
        builder.ToTable("ConfigurationApp");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.NomProprietairePRMP).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DateInstallation).IsRequired();
    }
}
