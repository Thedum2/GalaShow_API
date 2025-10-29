using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using GalaShow.Common.Models;

namespace GalaShow.Common.Errors
{
    public static class ErrorResults
    {
        public static APIGatewayProxyResponse? Json(ErrorCode code, string? msgOverride = null)
        {
            ErrorInfo errorInfo = ErrorCatalog.Get(code, msgOverride);
            var response = ApiResponse<object>.Fail(errorInfo);

            return new APIGatewayProxyResponse
            {
                StatusCode = errorInfo.HttpStatusCode,
                Headers = ResponseHeaders.Get(),
                Body = JsonSerializer.Serialize(response)
            };
        }
    }
}
