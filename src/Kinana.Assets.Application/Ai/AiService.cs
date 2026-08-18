using System.Diagnostics;
using System.Globalization;
using Kinana.AssetManagement.Application.Ai;
using Kinana.AssetManagement.Application.Assets;
using Kinana.AssetManagement.Application.Caching;
using Kinana.AssetManagement.Application.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kinana.AssetManagement.Application.Ai;

public sealed class AiService : IAiService
{
    private static readonly string[] KnownStatuses =
        ["Available", "Assigned", "Under Maintenance", "Retired"];

    private readonly IAiProvider _provider;
    private readonly IAssetReadRepository _repository;
    private readonly ICacheService _cache;
    private readonly CacheKeys _cacheKeys;
    private readonly CacheSettings _cacheSettings;
    private readonly AiSettings _aiSettings;
    private readonly ILogger<AiService> _logger;

    public AiService(
        IAiProvider provider,
        IAssetReadRepository repository,
        ICacheService cache,
        CacheKeys cacheKeys,
        IOptions<CacheSettings> cacheSettings,
        IOptions<AiSettings> aiSettings,
        ILogger<AiService> logger)
    {
        _provider = provider;
        _repository = repository;
        _cache = cache;
        _cacheKeys = cacheKeys;
        _cacheSettings = cacheSettings.Value;
        _aiSettings = aiSettings.Value;
        _logger = logger;
    }

    public async Task<AiChatResponse> AskAsync(AiChatRequest request, bool includeCost, CancellationToken ct)
    {
        var role = includeCost ? "Admin" : "User";
        var cacheKey = _cacheKeys.AiAnswer(role, request.Question);

        var cached = await _cache.GetAsync<AiChatResponse>(cacheKey, ct);
        if (cached is not null)
        {
            _logger.LogDebug("AI answer cache hit ({Key}).", cacheKey);
            return cached;
        }

        var stopwatch = Stopwatch.StartNew();
        var response = await AskCoreAsync(request.Question, includeCost, ct);
        stopwatch.Stop();

        _logger.LogInformation(
            "AI question answered by provider {Provider} in {ElapsedMs} ms ({Role} caller).",
            _provider.Name,
            stopwatch.ElapsedMilliseconds,
            role);

        await _cache.SetAsync(cacheKey, response, _cacheSettings.AiAnswerTtl, ct);

        return response;
    }

    private async Task<AiChatResponse> AskCoreAsync(string question, bool includeCost, CancellationToken ct)
    {
        var intent = await ExtractIntentAsync(question, ct);

        return intent.IntentType switch
        {
            "value" => await BuildValueResponseAsync(intent, includeCost, ct),
            "answer" => new AiChatResponse(
                string.IsNullOrWhiteSpace(intent.Answer)
                    ? "I'm not sure how to answer that. Try asking about your assets, for example: 'Show me all laptops assigned to Presales.'"
                    : intent.Answer,
                [],
                0),
            _ => await BuildSearchResponseAsync(intent, includeCost, ct)
        };
    }

    private async Task<AssetSearchIntent> ExtractIntentAsync(string question, CancellationToken ct)
    {
        try
        {
            var completion = await _provider.CompleteAsync(
                new AiCompletionRequest(SystemPrompts.AssetIntent, question),
                ct);

            return AiIntentParser.Parse(completion);
        }
        catch (ServiceUnavailableException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to interpret the AI provider response.");
            return new AssetSearchIntent
            {
                IntentType = "answer",
                Answer = "I couldn't understand that question. Try asking about your assets, for example: 'Show me all laptops assigned to Presales.'"
            };
        }
    }

    private async Task<AiChatResponse> BuildSearchResponseAsync(AssetSearchIntent intent, bool includeCost, CancellationToken ct)
    {
        var resolved = await ResolveQueryAsync(intent, ct);
        if (resolved.UnresolvedMessage is not null)
        {
            return new AiChatResponse(resolved.UnresolvedMessage, [], 0);
        }

        var filtered = AssetQueries.ApplyFilters(_repository.Assets.AsNoTracking(), resolved.Query!);
        var totalCount = await filtered.CountAsync(ct);

        var rows = await AssetQueries.Project(
                filtered
                    .OrderBy(a => a.AssetCode)
                    .Take(Math.Clamp(_aiSettings.MaxRows, 1, 100)),
                includeCost)
            .ToListAsync(ct);

        return new AiChatResponse(ComposeSearchAnswer(intent, rows, totalCount), rows, totalCount);
    }

    private async Task<AiChatResponse> BuildValueResponseAsync(AssetSearchIntent intent, bool includeCost, CancellationToken ct)
    {
        if (!includeCost)
        {
            return new AiChatResponse(
                "Purchase cost information is restricted to administrators, so I can't show cost figures for your account.",
                [],
                0);
        }

        var resolved = await ResolveQueryAsync(intent, ct);
        if (resolved.UnresolvedMessage is not null)
        {
            return new AiChatResponse(resolved.UnresolvedMessage, [], 0);
        }

        var filtered = AssetQueries.ApplyFilters(_repository.Assets.AsNoTracking(), resolved.Query!);
        var totalCount = await filtered.CountAsync(ct);
        var total = await filtered.SumAsync(a => (decimal?)a.PurchaseCost) ?? 0m;

        var rows = await AssetQueries.Project(
                filtered
                    .OrderBy(a => a.AssetCode)
                    .Take(Math.Clamp(_aiSettings.MaxRows, 1, 100)),
                includeCost: true)
            .ToListAsync(ct);

        var answer =
            $"The total purchase cost of the {totalCount} matching asset{(totalCount == 1 ? string.Empty : "s")} " +
            $"is {total.ToString("C", CultureInfo.InvariantCulture)}.";

        return new AiChatResponse(answer, rows, totalCount);
    }

    private async Task<ResolvedQuery> ResolveQueryAsync(AssetSearchIntent intent, CancellationToken ct)
    {
        var query = new SearchAssetsQuery
        {
            Page = 1,
            PageSize = Math.Clamp(_aiSettings.MaxRows, 1, 100),
            Search = string.IsNullOrWhiteSpace(intent.SearchTerm) ? null : intent.SearchTerm.Trim(),
            SortBy = "assetCode",
            SortDirection = "asc"
        };

        if (!string.IsNullOrWhiteSpace(intent.CategoryName))
        {
            var id = await _repository.Categories.AsNoTracking()
                .Where(c => c.Name.ToLower() == intent.CategoryName.Trim().ToLower())
                .Select(c => (int?)c.Id)
                .FirstOrDefaultAsync(ct);
            if (id is null)
            {
                return new ResolvedQuery(null, $"I couldn't find a category called '{intent.CategoryName}'.");
            }

            query.CategoryId = id;
        }

        if (!string.IsNullOrWhiteSpace(intent.AssetTypeName))
        {
            var id = await _repository.AssetTypes.AsNoTracking()
                .Where(t => t.Name.ToLower() == intent.AssetTypeName.Trim().ToLower())
                .Select(t => (int?)t.Id)
                .FirstOrDefaultAsync(ct);
            if (id is null)
            {
                return new ResolvedQuery(null, $"I couldn't find an asset type called '{intent.AssetTypeName}'.");
            }

            query.AssetTypeId = id;
        }

        if (!string.IsNullOrWhiteSpace(intent.DepartmentName))
        {
            var id = await _repository.Departments.AsNoTracking()
                .Where(d => d.Name.ToLower() == intent.DepartmentName.Trim().ToLower())
                .Select(d => (int?)d.Id)
                .FirstOrDefaultAsync(ct);
            if (id is null)
            {
                return new ResolvedQuery(null, $"I couldn't find a department called '{intent.DepartmentName}'.");
            }

            query.DepartmentId = id;
        }

        if (!string.IsNullOrWhiteSpace(intent.LocationName))
        {
            var id = await _repository.Locations.AsNoTracking()
                .Where(l => l.Name.ToLower() == intent.LocationName.Trim().ToLower())
                .Select(l => (int?)l.Id)
                .FirstOrDefaultAsync(ct);
            if (id is null)
            {
                return new ResolvedQuery(null, $"I couldn't find a location called '{intent.LocationName}'.");
            }

            query.LocationId = id;
        }

        if (!string.IsNullOrWhiteSpace(intent.AssignedEmployeeName))
        {
            var lower = intent.AssignedEmployeeName.Trim().ToLower();
            var id = await _repository.Employees.AsNoTracking()
                .Where(e => e.Name.ToLower().Contains(lower) || lower.Contains(e.Name.ToLower()))
                .Select(e => (int?)e.Id)
                .FirstOrDefaultAsync(ct);
            if (id is null)
            {
                return new ResolvedQuery(null, $"I couldn't find an employee called '{intent.AssignedEmployeeName}'.");
            }

            query.AssignedEmployeeId = id;
        }

        if (!string.IsNullOrWhiteSpace(intent.Status))
        {
            var status = KnownStatuses.FirstOrDefault(s =>
                string.Equals(s, intent.Status.Trim(), StringComparison.OrdinalIgnoreCase));
            if (status is null)
            {
                return new ResolvedQuery(null, $"I don't recognise the status '{intent.Status}'.");
            }

            query.Status = status;
        }

        return new ResolvedQuery(query, null);
    }

    private static string ComposeSearchAnswer(AssetSearchIntent intent, IReadOnlyList<AssetResponse> rows, int totalCount)
    {
        var clause = DescribeFilters(intent);

        if (intent.CountOnly)
        {
            return $"There {(totalCount == 1 ? "is" : "are")} {totalCount} matching asset{(totalCount == 1 ? string.Empty : "s")} ({clause}).";
        }

        if (totalCount == 0)
        {
            return $"I couldn't find any assets matching {clause}.";
        }

        var sample = rows.Take(10).Select(a => $"{a.AssetCode} ({a.AssetName})").ToList();
        var answer = $"Found {totalCount} matching asset{(totalCount == 1 ? string.Empty : "s")} ({clause}):\n{string.Join("\n", sample)}";

        var more = totalCount - sample.Count;
        if (more > 0)
        {
            answer += $"\n… and {more} more.";
        }

        return answer;
    }

    private static string DescribeFilters(AssetSearchIntent intent)
    {
        var parts = new List<string>();
        if (intent.SearchTerm is { Length: > 0 })
        {
            parts.Add($"'{intent.SearchTerm}'");
        }

        if (intent.CategoryName is { Length: > 0 })
        {
            parts.Add($"category {intent.CategoryName}");
        }

        if (intent.AssetTypeName is { Length: > 0 })
        {
            parts.Add($"type {intent.AssetTypeName}");
        }

        if (intent.Status is { Length: > 0 })
        {
            parts.Add($"status {intent.Status}");
        }

        if (intent.DepartmentName is { Length: > 0 })
        {
            parts.Add($"department {intent.DepartmentName}");
        }

        if (intent.LocationName is { Length: > 0 })
        {
            parts.Add($"location {intent.LocationName}");
        }

        if (intent.AssignedEmployeeName is { Length: > 0 })
        {
            parts.Add($"assigned to {intent.AssignedEmployeeName}");
        }

        return parts.Count > 0 ? string.Join(", ", parts) : "your criteria";
    }

    private sealed record ResolvedQuery(SearchAssetsQuery? Query, string? UnresolvedMessage);
}
