using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Data.Configurations;

/// <summary>Configuration EF Core de <see cref="SuiviExecution"/>.</summary>
public class SuiviExecutionConfiguration : IEntityTypeConfiguration<SuiviExecution>
{
    public void Configure(EntityTypeBuilder<SuiviExecution> builder)
    {
        builder.ToTable("SuiviExecution");

        builder.HasKey(x => x.IdSuivi);

        builder.Property(x => x.IdSuivi).ValueGeneratedOnAdd();
        builder.Property(x => x.RefMarche).HasMaxLength(50).IsRequired();
        builder.Property(x => x.RefTEF).HasMaxLength(100);

        builder.HasIndex(x => x.RefMarche).IsUnique();
    }
}
