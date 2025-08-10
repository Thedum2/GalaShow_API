using System;
using System.Collections.Generic;
using Amazon.Lambda.APIGatewayEvents;

public enum Stage
{
    None = 0,
    Dev,
    Prod
}
public static class StageResolver
{
    private static Stage CurrentStage { get; set; } = Stage.None;

    public static Stage Resolve(APIGatewayProxyRequest? req = null)
    {
        if (CurrentStage != Stage.None)
            return CurrentStage;

        var isLocal = string.Equals(Environment.GetEnvironmentVariable("AWS_SAM_LOCAL"), "true", StringComparison.OrdinalIgnoreCase);
        if (isLocal)
        {
            return CurrentStage = Stage.Dev;
        }

        string? stageName = null;
        var stageFromContext = req?.RequestContext?.Stage;
        Console.WriteLine("[Resolve] Context Stage = " + stageFromContext);
        if (!string.IsNullOrWhiteSpace(stageFromContext))
        {
            stageName = stageFromContext;
        }

        if (string.IsNullOrWhiteSpace(stageName))
        {
            var fromHost = ResolveFromHost(req?.Headers);
            Console.WriteLine("[Resolve] From Host = " + fromHost);
            if (!string.IsNullOrWhiteSpace(fromHost))
            {
                stageName = fromHost;
            }
        }
        
        CurrentStage = Parse(stageName) switch
        {
            Stage.None => Stage.Dev,
            var s => s
        };
        
        return CurrentStage;
    }
    private static string? ResolveFromHost(IDictionary<string, string>? headers)
    {
        if (headers == null) return null;

        headers.TryGetValue("Host", out var host);
        if (string.IsNullOrWhiteSpace(host))
            headers.TryGetValue("host", out host);

        if (string.IsNullOrWhiteSpace(host)) return null;

        if (host.Contains("api-dev.galashow.xyz", StringComparison.OrdinalIgnoreCase)) return "dev";
        if (host.Contains("api.galashow.xyz", StringComparison.OrdinalIgnoreCase)) return "prod";

        var parts = host.Split('.');
        if (parts.Length >= 3 && parts[0].StartsWith("api-", StringComparison.OrdinalIgnoreCase))
            return parts[0].Substring("api-".Length);

        return null;
    }

    private static Stage Parse(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return Stage.None;

        var v = s.Trim().ToLowerInvariant();
        if (v is "dev" or "development") return Stage.Dev;
        if (v is "prod" or "production") return Stage.Prod;

        return Stage.None;
    }

    public static bool IsDev(string stage) =>
        stage.Equals("dev", StringComparison.OrdinalIgnoreCase) ||
        stage.Equals("development", StringComparison.OrdinalIgnoreCase);

    public static bool IsProd(string stage) =>
        stage.Equals("prod", StringComparison.OrdinalIgnoreCase) ||
        stage.Equals("production", StringComparison.OrdinalIgnoreCase);

    public static bool IsDev() => CurrentStage == Stage.Dev;
    public static bool IsProd() => CurrentStage == Stage.Prod;

    /// <summary>테스트/리셋용: 캐시 무효화</summary>
    public static void Invalidate() => CurrentStage = Stage.None;
}
