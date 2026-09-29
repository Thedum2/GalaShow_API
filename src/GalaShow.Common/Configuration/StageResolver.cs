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
    private static Stage _current = Stage.None;

    public static Stage Resolve(APIGatewayProxyRequest? req = null)
    {
        // Deployment configuration is authoritative, including for warm invocations.
        var configured = Environment.GetEnvironmentVariable("STAGE");
        if (configured is not null)
        {
            _current = Parse(configured);
            if (_current == Stage.None)
                throw new InvalidOperationException("STAGE must be dev or prod.");
            return _current;
        }

        if (string.Equals(Environment.GetEnvironmentVariable("AWS_SAM_LOCAL"), "true", StringComparison.OrdinalIgnoreCase))
            return _current = Stage.Dev;

        var gatewayStage = Parse(req?.RequestContext?.Stage);
        if (gatewayStage != Stage.None)
            return _current = gatewayStage;

        var hostStage = ResolveFromHost(req?.Headers);
        return _current = hostStage == Stage.None ? Stage.Prod : hostStage;
    }

    private static Stage ResolveFromHost(IDictionary<string, string>? headers)
    {
        var host = headers?.FirstOrDefault(h => h.Key.Equals("Host", StringComparison.OrdinalIgnoreCase)).Value;
        if (string.Equals(host, "api-dev.galashow.cloud", StringComparison.OrdinalIgnoreCase)) return Stage.Dev;
        if (string.Equals(host, "api.galashow.cloud", StringComparison.OrdinalIgnoreCase)) return Stage.Prod;
        return Stage.None;
    }

    private static Stage Parse(string? stage) => stage?.Trim().ToLowerInvariant() switch
    {
        "dev" or "development" => Stage.Dev,
        "prod" or "production" => Stage.Prod,
        _ => Stage.None
    };

    public static bool IsDev(string stage) => Parse(stage) == Stage.Dev;
    public static bool IsProd(string stage) => Parse(stage) == Stage.Prod;
    public static bool IsDev() => _current == Stage.Dev;
    public static bool IsProd() => _current == Stage.Prod;
    public static void Invalidate() => _current = Stage.None;
}
