namespace Kinana.AssetManagement.Application.Caching;

public sealed class CacheSettings
{
    public const string SectionName = "CacheSettings";

    public string ConnectionString { get; set; } = "localhost:6379";

    public string GlobalPrefix { get; set; } = "KinanaAssets:";

    public int LookupTtlMinutes { get; set; } = 60;

    public int AssetTtlMinutes { get; set; } = 15;

    public int AiAnswerTtlMinutes { get; set; } = 10;

    public TimeSpan LookupTtl => TimeSpan.FromMinutes(LookupTtlMinutes);

    public TimeSpan AssetTtl => TimeSpan.FromMinutes(AssetTtlMinutes);

    public TimeSpan AiAnswerTtl => TimeSpan.FromMinutes(AiAnswerTtlMinutes);
}
