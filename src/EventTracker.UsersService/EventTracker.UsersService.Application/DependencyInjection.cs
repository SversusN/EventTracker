using EventTracker.UsersService.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EventTracker.UsersService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();

        return services;
    }
}
