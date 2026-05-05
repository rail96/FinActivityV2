using FindActivity.Application;
using FindActivity.Domain.Entities;
using FindActivity.Infrastructure;
using FindActivity.Infrastructure.Data;
using FindActivity.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container. AddInfrastructure registers AppDbContext; no need to register it again here.
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    {
        // TODO: flip back to true once we've set up domain authentication in SendGrid
        // (sending from a Gmail address gets blocked by iCloud/Outlook/Yahoo via DMARC).
        // See: https://docs.sendgrid.com/ui/account-and-settings/how-to-set-up-domain-authentication
        options.SignIn.RequireConfirmedAccount = false;

        // Reasonable password floor for an MVP. Tighten later if needed.
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddRoles<IdentityRole>()
    .AddDefaultTokenProviders()
    .AddEntityFrameworkStores<AppDbContext>();

// Email confirmation + password reset tokens live for 24 hours (default is 1 day as well, but we set it explicitly).
builder.Services.Configure<DataProtectionTokenProviderOptions>(o =>
    o.TokenLifespan = TimeSpan.FromHours(24));

// SendGrid-backed email sender for Identity confirmation + password reset emails.
builder.Services.Configure<EmailSenderOptions>(builder.Configuration.GetSection("SendGrid"));
builder.Services.AddTransient<IEmailSender, EmailSender>();

// Higher-level activity notifications (RSVP, cancellation, reminders).
builder.Services.AddTransient<INotificationService, NotificationService>();

// Sends a reminder email ~24 hours before each scheduled activity starts.
builder.Services.AddHostedService<ActivityReminderService>();
// Ensures the Admin role exists, and promotes Admin:BootstrapEmail (if set) to Admin on startup.
builder.Services.AddHostedService<RoleSeeder>();

builder.Services.AddApplication();
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
