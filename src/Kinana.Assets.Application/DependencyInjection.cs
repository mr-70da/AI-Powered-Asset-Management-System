using Kinana.AssetManagement.Application.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace Kinana.AssetManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserAdminService, UserAdminService>();

        return services;
    }
}
