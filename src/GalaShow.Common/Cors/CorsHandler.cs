using System.Diagnostics.CodeAnalysis;
using Amazon.Lambda.APIGatewayEvents;

namespace GalaShow.Common.Cors;

public static class CorsHandler
{
    private static HashSet<string> _allowedOrigins = new(StringComparer.Ordinal);

    public static Task InitializeAsync()
    {
        var configured = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS");
        var defaults = StageResolver.IsDev()
            ? "https://dev.galashow.cloud,https://admin-dev.galashow.cloud"
            : "https://galashow.cloud,https://admin.galashow.cloud";
        _allowedOrigins = (configured ?? defaults)
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Where(origin => StageResolver.IsDev() || !Uri.TryCreate(origin, UriKind.Absolute, out var uri) || !uri.IsLoopback)
            .ToHashSet(StringComparer.Ordinal);
        return Task.CompletedTask;
    }

    [return: NotNullIfNotNull(nameof(response))]
    public static APIGatewayProxyResponse? AddCorsHeaders(APIGatewayProxyRequest request, APIGatewayProxyResponse? response)
    {
        if (response is null) return null;
        response.Headers ??= new Dictionary<string, string>();

        // The proxy must apply our allowlist even when the upstream sends CORS headers.
        foreach (var key in response.Headers.Keys.Where(key => key.StartsWith("Access-Control-", StringComparison.OrdinalIgnoreCase)).ToArray())
            response.Headers.Remove(key);

        var vary = response.Headers.FirstOrDefault(h => h.Key.Equals("Vary", StringComparison.OrdinalIgnoreCase));
        var varyValues = (vary.Value ?? "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (!varyValues.Contains("Origin", StringComparer.OrdinalIgnoreCase))
            response.Headers[vary.Key ?? "Vary"] = string.IsNullOrWhiteSpace(vary.Value) ? "Origin" : $"{vary.Value}, Origin";

        var origin = request.Headers?.FirstOrDefault(h => h.Key.Equals("Origin", StringComparison.OrdinalIgnoreCase)).Value;
        if (origin is not null &&
            (_allowedOrigins.Contains(origin) || (StageResolver.IsDev() && IsLocalhostOrigin(origin))))
        {
            response.Headers["Access-Control-Allow-Origin"] = origin;
            response.Headers["Access-Control-Allow-Credentials"] = "true";
            response.Headers["Access-Control-Allow-Headers"] = "Content-Type,X-Amz-Date,Authorization,X-Api-Key,X-Amz-Security-Token,Client-Id,Client-Secret";
            response.Headers["Access-Control-Allow-Methods"] = "GET,POST,PUT,DELETE,OPTIONS";
        }

        return response;
    }

    private static bool IsLocalhostOrigin(string origin) =>
        Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
        uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase);
}
