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
using GalaShow.Common.Configuration;
using GalaShow.Common.Cors;
using GalaShow.Common.Errors;
using GalaShow.Common.Infrastructure;
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
            StageResolver.Resolve(request);
            await AppBootstrap.InitAsync();

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

                if (!string.IsNullOrEmpty(request.Body))
                {
                    // API Gateway가 바이너리 데이터를 base64로 인코딩했는지 확인
                    byte[] bodyBytes;
                    if (request.IsBase64Encoded)
                    {
                        bodyBytes = Convert.FromBase64String(request.Body);
                    }
                    else
                    {
                        bodyBytes = Encoding.UTF8.GetBytes(request.Body);
                    }

                    httpRequest.Content = new ByteArrayContent(bodyBytes);

                    // Content-Type을 Headers 또는 MultiValueHeaders에서 찾기
                    string requestContentType = null;

                    if (request.Headers != null && request.Headers.TryGetValue("Content-Type", out var ct))
                    {
                        requestContentType = ct;
                    }
                    else if (request.Headers != null && request.Headers.TryGetValue("content-type", out var ctLower))
                    {
                        requestContentType = ctLower;
                    }
                    else if (request.MultiValueHeaders != null && request.MultiValueHeaders.TryGetValue("Content-Type", out var mvCt))
                    {
                        requestContentType = string.Join(", ", mvCt);
                    }
                    else if (request.MultiValueHeaders != null && request.MultiValueHeaders.TryGetValue("content-type", out var mvCtLower))
                    {
                        requestContentType = string.Join(", ", mvCtLower);
                    }

                    if (!string.IsNullOrEmpty(requestContentType))
                    {
                        httpRequest.Content.Headers.TryAddWithoutValidation("Content-Type", requestContentType);
                    }
                    else
                    {
                        httpRequest.Content.Headers.TryAddWithoutValidation("Content-Type", "application/json");
                    }
                }

                if (request.Headers != null)
                {
                    foreach (var header in request.Headers)
                    {
                        var headerKey = header.Key.ToLower();
                        if (headerKey == "host" || headerKey == "content-length" || headerKey == "content-type" ||
                            headerKey == "origin" || headerKey == "referer")
                            continue;

                        httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }

                var chzzkResponse = await HttpClient.SendAsync(httpRequest);
                var responseBody = await chzzkResponse.Content.ReadAsStringAsync();

                context.Logger.LogInformation($"Response status: {(int)chzzkResponse.StatusCode}");

                // API의 응답 헤더를 가져옴 (hop-by-hop 헤더는 제외)
                var responseHeaders = new Dictionary<string, string>();
                foreach (var header in chzzkResponse.Headers)
                {
                    var headerKey = header.Key.ToLower();
                    if (headerKey == "transfer-encoding" || headerKey == "connection" ||
                        headerKey == "keep-alive" || headerKey == "proxy-authenticate" ||
                        headerKey == "proxy-authorization" || headerKey == "te" ||
                        headerKey == "trailers" || headerKey == "upgrade")
                        continue;

                    responseHeaders[header.Key] = string.Join(", ", header.Value);
                }
                foreach (var header in chzzkResponse.Content.Headers)
                {
                    var headerKey = header.Key.ToLower();
                    if (headerKey == "content-length")
                        continue;

                    responseHeaders[header.Key] = string.Join(", ", header.Value);
                }

                response = new APIGatewayProxyResponse
                {
                    StatusCode = (int)chzzkResponse.StatusCode,
                    Headers = responseHeaders,
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
