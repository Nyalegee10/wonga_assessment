using Microsoft.Extensions.DependencyInjection;
using UserAuth.Application.Interfaces;
using UserAuth.Application.Services;

namespace UserAuth.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        return services;
    }
}
