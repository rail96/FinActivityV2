using FindActivity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FindActivity.Infrastructure.Data.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Comment).HasMaxLength(2000);
        builder.Property(r => r.ReviewerUserId).HasMaxLength(450).IsRequired();
        builder.Property(r => r.RevieweeUserId).HasMaxLength(450).IsRequired();

        builder.HasIndex(r => new { r.ActivityId, r.ReviewerUserId }).IsUnique();

        builder.HasOne(r => r.Activity)
            .WithMany(a => a.Reviews)
            .HasForeignKey(r => r.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.ReviewerUser)
            .WithMany()
            .HasForeignKey(r => r.ReviewerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.RevieweeUser)
            .WithMany()
            .HasForeignKey(r => r.RevieweeUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
