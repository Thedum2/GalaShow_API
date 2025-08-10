using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using GalaShow.Common.Models;

namespace GalaShow.Common.Errors
{
    public static class ErrorResults
    {
        public static APIGatewayProxyResponse Json(ErrorCode code, string? message = null, IEnumerable<string>? details = null, IDictionary<string,string>? extraHeaders = null)
        {
            var info = ErrorCatalog.Get(code, message);

            var headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json; charset=utf-8",
                ["Access-Control-Allow-Origin"] = "*"
            };

            headers["X-Error-Code"] = info.Code.ToString();

            if (info.AddWwwAuthenticateHeader)
            {
                headers["WWW-Authenticate"] = "Bearer error=\"invalid_token\", error_description=\"The access token expired\"";
            }

            if (extraHeaders != null)
            {
                foreach (var kv in extraHeaders) headers[kv.Key] = kv.Value;
            }

            var body = JsonSerializer.Serialize(ApiResponse<object>.ErrorResult(info.Message, details?.ToList(), info.HttpStatus.ToString()));
            return new APIGatewayProxyResponse
            {
                StatusCode = info.HttpStatus,
                Headers = headers,
                Body = body
            };
        }
    }
}