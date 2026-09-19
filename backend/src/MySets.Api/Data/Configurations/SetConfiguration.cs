using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MySets.Api.Data.Entities;

namespace MySets.Api.Data.Configurations;

public class SetConfiguration : IEntityTypeConfiguration<Set>
{
    public void Configure(EntityTypeBuilder<Set> builder)
    {
        builder.HasKey(s => s.RebrickableSetNum);

        builder.HasIndex(s => s.Name)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");
    }
}
