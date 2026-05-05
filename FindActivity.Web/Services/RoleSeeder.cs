using FindActivity.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace FindActivity.Web.Services;

/// <summary>
/// Runs once at startup to:
///   1. Ensure the well-known roles (currently just "Admin") exist in AspNetRoles.
///   2. Promote a configured bootstrap user (Admin:BootstrapEmail) to the Admin role,
///      so the very first admin can be set without poking the database manually.
/// </summary>
public class RoleSeeder : IHostedService
{
    public const string AdminRole = "Admin";

    private readonly IServiceProvider _services;
    private readonly ILogger<RoleSeeder> _logger;
    private readonly IConfiguration _configuration;

    public RoleSeeder(IServiceProvider services, ILogger<RoleSeeder> logger, IConfiguration configuration)
    {
        _services = services;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (!await roleManager.RoleExistsAsync(AdminRole))
        {
            var result = await roleManager.CreateAsync(new IdentityRole(AdminRole));
            if (!result.Succeeded)
            {
                _logger.LogError(
                    "Failed to create '{Role}' role: {Errors}",
                    AdminRole,
                    string.Join("; ", result.Errors.Select(e => e.Description)));
                return;
            }
            _logger.LogInformation("Created '{Role}' role.", AdminRole);
        }

        var bootstrapEmail = _configuration["Admin:BootstrapEmail"];
        if (string.IsNullOrWhiteSpace(bootstrapEmail))
        {
            return;
        }

        var user = await userManager.FindByEmailAsync(bootstrapEmail);
        if (user is null)
        {
            _logger.LogInformation(
                "Admin:BootstrapEmail is set to '{Email}' but no matching user exists yet. " +
                "Register that account; on the next startup it will be promoted to Admin.",
                bootstrapEmail);
            return;
        }

        if (!await userManager.IsInRoleAsync(user, AdminRole))
        {
            var result = await userManager.AddToRoleAsync(user, AdminRole);
            if (result.Succeeded)
            {
                _logger.LogInformation("Granted '{Role}' role to bootstrap user {Email}.", AdminRole, bootstrapEmail);
            }
            else
            {
                _logger.LogError(
                    "Failed to add bootstrap user {Email} to '{Role}': {Errors}",
                    bootstrapEmail, AdminRole,
                    string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
