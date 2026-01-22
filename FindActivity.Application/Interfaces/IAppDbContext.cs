using FindActivity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FindActivity.Application.Interfaces;

public interface IAppDbContext
{
    DbSet<Activity> Activities { get; }
    DbSet<ActivityParticipant> ActivityParticipants { get; }
    DbSet<Review> Reviews { get; }
    DbSet<Category> Categories { get; }
    DbSet<ApplicationUser> Users { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
