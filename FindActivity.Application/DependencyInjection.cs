using FindActivity.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FindActivity.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IActivityService, ActivityService>();
        services.AddScoped<IReviewService, ReviewService>();
        return services;
    }
}
