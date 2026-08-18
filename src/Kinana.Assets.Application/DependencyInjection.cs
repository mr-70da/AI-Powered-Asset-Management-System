using FluentValidation;
using Kinana.AssetManagement.Application.Ai;
using Kinana.AssetManagement.Application.Assets;
using Kinana.AssetManagement.Application.Auth;
using Kinana.AssetManagement.Application.Lookups;
using Microsoft.Extensions.DependencyInjection;

namespace Kinana.AssetManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IValidator<CreateAssetRequest>, CreateAssetRequestValidator>();
        services.AddScoped<IValidator<UpdateAssetRequest>, UpdateAssetRequestValidator>();
        services.AddScoped<IValidator<TransferAssetRequest>, TransferAssetRequestValidator>();
        services.AddScoped<IValidator<AiChatRequest>, AiChatRequestValidator>();

        return services;
    }
}
