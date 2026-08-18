using Kinana.AssetManagement.Application.Assets;
using Kinana.AssetManagement.Application.Caching;
using Kinana.AssetManagement.Application.Lookups;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Kinana.AssetManagement.Infrastructure.Services;

public sealed class LookupService : ILookupService
{
    private readonly IAssetRepository _repository;
    private readonly ICacheService _cache;
    private readonly CacheKeys _cacheKeys;
    private readonly CacheSettings _settings;

    public LookupService(
        IAssetRepository repository,
        ICacheService cache,
        CacheKeys cacheKeys,
        IOptions<CacheSettings> settings)
    {
        _repository = repository;
        _cache = cache;
        _cacheKeys = cacheKeys;
        _settings = settings.Value;
    }

    public async Task<LookupsResponse> GetLookupsAsync(CancellationToken ct)
    {
        var key = _cacheKeys.Lookups();

        var cached = await _cache.GetAsync<LookupsResponse>(key, ct);
        if (cached is not null)
        {
            return cached;
        }

        var categories = await _repository.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new LookupItemDto(c.Id, c.Name))
            .ToListAsync(ct);

        var assetTypes = await _repository.AssetTypes
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new LookupItemDto(t.Id, t.Name))
            .ToListAsync(ct);

        var departments = await _repository.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new LookupItemDto(d.Id, d.Name))
            .ToListAsync(ct);

        var locations = await _repository.Locations
            .AsNoTracking()
            .OrderBy(l => l.Name)
            .Select(l => new LookupItemDto(l.Id, l.Name))
            .ToListAsync(ct);

        var employees = await _repository.Employees
            .AsNoTracking()
            .Where(e => e.IsActive)
            .OrderBy(e => e.Name)
            .Select(e => new LookupItemDto(e.Id, e.Name))
            .ToListAsync(ct);

        var response = new LookupsResponse(categories, assetTypes, departments, locations, employees);

        await _cache.SetAsync(key, response, _settings.LookupTtl, ct);

        return response;
    }
}
