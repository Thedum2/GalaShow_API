using Amazon.Lambda.APIGatewayEvents;
using GalaShow.Common.Configuration;
using GalaShow.Common.Service;

namespace GalaShow.Common.Cors;

public static class CorsHandler
{
    private static List<string> _allowedOrigins = new();

    public static async Task InitializeAsync()
    {
        const string secretArn = "arn:aws:secretsmanager:ap-northeast-2:610495549763:secret:galashow/cors-JUg1XU";
        _allowedOrigins = await SecretsService.Instance.GetCorsAllowedOriginsAsync(secretArn, StageResolver.IsDev()?"dev":"prod");
    }

    public static APIGatewayProxyResponse? AddCorsHeaders(APIGatewayProxyRequest request, APIGatewayProxyResponse? response)
    {
        var origin = request.Headers.FirstOrDefault(h => h.Key.Equals("origin", StringComparison.OrdinalIgnoreCase)).Value ?? "";

        if (_allowedOrigins.Contains(origin))
        {
            response.Headers["Access-Control-Allow-Origin"] = origin;
            response.Headers["Access-Control-Allow-Credentials"] = "true";
            response.Headers["Access-Control-Allow-Headers"] = "Content-Type,X-Amz-Date,Authorization,X-Api-Key,X-Amz-Security-Token,Client-Id,Client-Secret";
            response.Headers["Access-Control-Allow-Methods"] = "GET,POST,PUT,DELETE,OPTIONS";
        }

        return response;
    }
}
