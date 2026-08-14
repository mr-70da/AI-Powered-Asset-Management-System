using Kinana.AssetManagement.Application.Assets;
using Kinana.AssetManagement.Application.Auth;
using Kinana.AssetManagement.Application.Caching;
using Kinana.AssetManagement.Infrastructure.Caching;
using Kinana.AssetManagement.Infrastructure.Data;
using Kinana.AssetManagement.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kinana.AssetManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AssetManagementDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IAssetRepository, AssetRepository>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        services.Configure<CacheSettings>(configuration.GetSection(CacheSettings.SectionName));
        services.AddSingleton<CacheKeys>();
        services.AddSingleton<ICacheService, RedisCacheService>();

        return services;
    }
}
