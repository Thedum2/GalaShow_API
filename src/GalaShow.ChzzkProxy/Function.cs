using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using GalaShow.Common;
using GalaShow.Common.Cors;
using GalaShow.Common.Errors;
using GalaShow.Common.Models;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace GalaShow.ChzzkProxy
{
    public class Function
    {
        private static readonly HttpClient HttpClient = new();
        private const string ChzzkApiBase = "https://openapi.chzzk.naver.com";

        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            APIGatewayProxyResponse response;
            
            if (request.HttpMethod == "OPTIONS")
            {
                return CorsHandler.AddCorsHeaders(request, Success200());
            }

            try
            {
                var path = request.Path ?? "/";
                if (path.StartsWith("/chzzk", StringComparison.OrdinalIgnoreCase))
                {
                    path = path.Substring(6);
                }
                if (string.IsNullOrEmpty(path))
                {
                    path = "/";
                }

                var queryString = string.Empty;

                if (request.QueryStringParameters != null && request.QueryStringParameters.Any())
                {
                    queryString = "?" + string.Join("&",
                        request.QueryStringParameters.Select(kvp =>
                            $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value ?? string.Empty)}"));
                }

                var targetUrl = $"{ChzzkApiBase}{path}{queryString}";

                context.Logger.LogInformation($"Proxying request to: {targetUrl}");

                var httpRequest = new HttpRequestMessage
                {
                    Method = new HttpMethod(request.HttpMethod),
                    RequestUri = new Uri(targetUrl)
                };
                
                var contentType = "application/json";
                if (request.Headers != null && request.Headers.TryGetValue("Content-Type", out var requestContentType))
                {
                    contentType = requestContentType;
                }

                if (!string.IsNullOrEmpty(request.Body))
                {
                    httpRequest.Content = new StringContent(request.Body, Encoding.UTF8, contentType);
                }

                if (request.Headers != null)
                {
                    foreach (var header in request.Headers)
                    {
                        var headerKey = header.Key.ToLower();
                        if (headerKey == "host" || headerKey == "content-length" || headerKey == "content-type")
                            continue;

                        httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }

                var chzzkResponse = await HttpClient.SendAsync(httpRequest);
                var responseBody = await chzzkResponse.Content.ReadAsStringAsync();

                context.Logger.LogInformation($"Response status: {(int)chzzkResponse.StatusCode}");

                response = new APIGatewayProxyResponse
                {
                    StatusCode = (int)chzzkResponse.StatusCode,
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } },
                    Body = responseBody
                };
            }
            catch (HttpRequestException ex)
            {
                context.Logger.LogError($"HTTP request error: {ex.Message}");
                response = ErrorResults.Json(ErrorCode.Internal);
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"HTTP request error: {ex.Message}");
                response = ErrorResults.Json(ErrorCode.Internal);
            }

            return CorsHandler.AddCorsHeaders(request, response);
        }
        
        #region !============================ Helpers ============================!

        private static Dictionary<string, string> JsonHeaders() => ResponseHeaders.Get();

        private static APIGatewayProxyResponse Success200() => new()
        {
            StatusCode = 200,
            Headers = JsonHeaders(),
            Body = JsonSerializer.Serialize(ApiResponse<object>.Success())
        };

        #endregion
    }
}
