using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using GalaShow.Common.Models;

namespace GalaShow.Common.Errors
{
    public static class ErrorResults
    {
        public static APIGatewayProxyResponse Json(ErrorCode code, string? msgOverride = null)
        {
            ErrorInfo errorInfo = ErrorCatalog.Get(code, msgOverride);
            var response = ApiResponse<object>.Fail(errorInfo);
            
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)errorInfo.Code,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json; charset=utf-8" } },
                Body = JsonSerializer.Serialize(response)
            };
        }
    }
}
