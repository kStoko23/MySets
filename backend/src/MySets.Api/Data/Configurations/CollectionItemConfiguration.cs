using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MySets.Api.Data.Entities;

namespace MySets.Api.Data.Configurations;

public class CollectionItemConfiguration : IEntityTypeConfiguration<CollectionItem>
{
    public void Configure(EntityTypeBuilder<CollectionItem> builder)
    {
        builder.HasKey(ci => ci.Id);

        builder.HasIndex(ci => new { ci.UserId, ci.SetId }).IsUnique();

        builder.HasOne(ci => ci.User)
            .WithMany(u => u.CollectionItems)
            .HasForeignKey(ci => ci.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ci => ci.Set)
            .WithMany(s => s.CollectionItems)
            .HasForeignKey(ci => ci.SetId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
