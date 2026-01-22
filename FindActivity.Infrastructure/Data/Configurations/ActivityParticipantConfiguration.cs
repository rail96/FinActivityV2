using FindActivity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FindActivity.Infrastructure.Data.Configurations;

public class ActivityParticipantConfiguration : IEntityTypeConfiguration<ActivityParticipant>
{
    public void Configure(EntityTypeBuilder<ActivityParticipant> builder)
    {
        builder.HasKey(ap => new { ap.ActivityId, ap.UserId });
        builder.Property(ap => ap.UserId).HasMaxLength(450).IsRequired();

        builder.HasOne(ap => ap.Activity)
            .WithMany(a => a.Participants)
            .HasForeignKey(ap => ap.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ap => ap.User)
            .WithMany()
            .HasForeignKey(ap => ap.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
