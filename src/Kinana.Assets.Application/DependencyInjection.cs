using FluentValidation;
using Kinana.AssetManagement.Application.Assets;
using Kinana.AssetManagement.Application.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace Kinana.AssetManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserAdminService, UserAdminService>();

        services.AddScoped<IValidator<CreateAssetRequest>, CreateAssetRequestValidator>();
        services.AddScoped<IValidator<UpdateAssetRequest>, UpdateAssetRequestValidator>();

        return services;
    }
}
