using FindActivity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FindActivity.Infrastructure.Data.Configurations;

public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.ReporterUserId).HasMaxLength(450).IsRequired();
        builder.Property(r => r.TargetUserId).HasMaxLength(450);
        builder.Property(r => r.ResolvedByUserId).HasMaxLength(450);

        builder.Property(r => r.Details).HasMaxLength(2000);
        builder.Property(r => r.ResolutionNotes).HasMaxLength(2000);

        // Indexes for moderator queries: open reports, reports by target.
        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.TargetActivityId);
        builder.HasIndex(r => r.TargetUserId);

        // Reporter -> Reports. Restrict to keep an audit trail even if the reporter is deleted.
        builder.HasOne(r => r.ReporterUser)
            .WithMany()
            .HasForeignKey(r => r.ReporterUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Optional Activity target. Cascade so deleting an activity tidies up its reports.
        builder.HasOne(r => r.TargetActivity)
            .WithMany()
            .HasForeignKey(r => r.TargetActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        // Optional User target. Restrict so the report sticks even if the user is deleted.
        builder.HasOne(r => r.TargetUser)
            .WithMany()
            .HasForeignKey(r => r.TargetUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Resolver (admin) link, populated when an admin closes the report.
        builder.HasOne(r => r.ResolvedByUser)
            .WithMany()
            .HasForeignKey(r => r.ResolvedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
