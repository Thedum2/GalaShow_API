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

    /// <summary>
    /// 현재 스테이지를 판별하여 캐시합니다.
    /// 우선순위: SAM 로컬 → RequestContext.Stage → Host → Env → 함수명 → 기본(Dev)
    /// </summary>
    public static Stage Resolve(APIGatewayProxyRequest? req = null)
    {
        if (_current != Stage.None) return _current;

        // 0) SAM 로컬 실행이면 무조건 dev
        var isLocal = string.Equals(
            Environment.GetEnvironmentVariable("AWS_SAM_LOCAL"),
            "true",
            StringComparison.OrdinalIgnoreCase
        );
        if (isLocal)
            return _current = Stage.Dev;

        string? stageName = null;

        // 1) API Gateway Stage
        var stageFromContext = req?.RequestContext?.Stage;
        Console.WriteLine("[Resolve] Context Stage = " + stageFromContext);
        if (!string.IsNullOrWhiteSpace(stageFromContext))
            stageName = stageFromContext;

        // 2) Host 헤더
        if (string.IsNullOrWhiteSpace(stageName))
        {
            var fromHost = ResolveFromHost(req?.Headers);
            Console.WriteLine("[Resolve] From Host = " + fromHost);
            if (!string.IsNullOrWhiteSpace(fromHost))
                stageName = fromHost;
        }

        // 3) 환경변수
        if (string.IsNullOrWhiteSpace(stageName))
        {
            var fromEnv = ResolveFromEnv();
            Console.WriteLine("[Resolve] From Env = " + fromEnv);
            if (!string.IsNullOrWhiteSpace(fromEnv))
                stageName = fromEnv;
        }

        // 4) Lambda 함수명
        if (string.IsNullOrWhiteSpace(stageName))
        {
            var fromFn = ResolveFromFunctionName();
            Console.WriteLine("[Resolve] From FunctionName = " + fromFn);
            if (!string.IsNullOrWhiteSpace(fromFn))
                stageName = fromFn;
        }

        // 5) 최종 결정 (기본: Dev)
        _current = Parse(stageName) switch
        {
            Stage.None => Stage.Dev,
            var s => s
        };

        Console.WriteLine("[Resolve] Final Stage = " + _current);
        return _current;
    }

    private static string? ResolveFromHost(IDictionary<string, string>? headers)
    {
        if (headers == null) return null;

        string? host = null;
        if (!headers.TryGetValue("Host", out host))
            headers.TryGetValue("host", out host);

        if (string.IsNullOrWhiteSpace(host)) return null;

        if (host.Contains("api-dev.galashow.xyz", StringComparison.OrdinalIgnoreCase)) return "dev";
        if (host.Contains("api.galashow.xyz", StringComparison.OrdinalIgnoreCase)) return "prod";

        // api-<stage>.galashow.xyz 패턴 대응
        var parts = host.Split('.');
        if (parts.Length >= 3 && parts[0].StartsWith("api-", StringComparison.OrdinalIgnoreCase))
            return parts[0].Substring("api-".Length);

        return null;
    }

    private static string? ResolveFromEnv()
    {
        var s =
            Environment.GetEnvironmentVariable("STAGE") ??
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ??
            Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        return string.IsNullOrWhiteSpace(s) ? null : s;
    }

    private static string? ResolveFromFunctionName()
    {
        var fn = Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME");
        if (string.IsNullOrWhiteSpace(fn)) return null;

        if (fn.Contains("dev", StringComparison.OrdinalIgnoreCase)) return "dev";
        if (fn.Contains("prod", StringComparison.OrdinalIgnoreCase)) return "prod";
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

    // 편의 메서드들
    public static bool IsDev(string stage) =>
        stage.Equals("dev", StringComparison.OrdinalIgnoreCase) ||
        stage.Equals("development", StringComparison.OrdinalIgnoreCase);

    public static bool IsProd(string stage) =>
        stage.Equals("prod", StringComparison.OrdinalIgnoreCase) ||
        stage.Equals("production", StringComparison.OrdinalIgnoreCase);

    public static bool IsDev() => _current == Stage.Dev;
    public static bool IsProd() => _current == Stage.Prod;

    /// <summary>테스트/리셋용: 캐시 초기화</summary>
    public static void Invalidate() => _current = Stage.None;
}
