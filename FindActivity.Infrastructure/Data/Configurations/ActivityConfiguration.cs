using FindActivity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FindActivity.Infrastructure.Data.Configurations;

public class ActivityConfiguration : IEntityTypeConfiguration<Activity>
{
    public void Configure(EntityTypeBuilder<Activity> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Title).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Description).HasMaxLength(2000).IsRequired();
        builder.Property(a => a.Address).HasMaxLength(300).IsRequired();
        builder.Property(a => a.City).HasMaxLength(120).IsRequired();
        builder.Property(a => a.State).HasMaxLength(80).IsRequired();
        builder.Property(a => a.AddressPlaceId).HasMaxLength(150);
        builder.Property(a => a.CreatedByUserId).HasMaxLength(450).IsRequired();

        builder.HasIndex(a => a.City);
        builder.HasIndex(a => a.CategoryId);
        builder.HasIndex(a => a.StartUtc);

        builder.HasOne(a => a.Category)
            .WithMany()
            .HasForeignKey(a => a.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.CreatedByUser)
            .WithMany()
            .HasForeignKey(a => a.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
