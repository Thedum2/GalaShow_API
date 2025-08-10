using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using GalaShow.Common.Infrastructure;
using GalaShow.Common.Models;
using GalaShow.Common.Models.Request.Banner;
using GalaShow.Common.Models.Response.Banner;
using GalaShow.Common.Service;
using Microsoft.IdentityModel.Tokens;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace GalaShow.Banner
{
    public class Function
    {
        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request,
            ILambdaContext context)
        {
            await AppBootstrap.InitAsync();

            try
            {
                return (request.HttpMethod, request.Path) switch
                {
                    ("GET", "/banners") => await GetAllBanners(),

                    ("PUT", var p) when p.StartsWith("/banners/") => (await TokenService.Instance.RequireAuthThen(
                                                                         request, _ => UpdateBanner(request)))
                                                                     ?? Unauthorized(),

                    _ => CreateNotFoundResponse()
                };
            }
            catch (SecurityTokenException ste)
            {
                context.Logger.LogError($"Auth error: {ste.Message}");
                return Unauthorized();
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Error: {ex}");
                return CreateErrorResponse("Internal server error");
            }
        }


        private async Task<APIGatewayProxyResponse> GetAllBanners()
        {
            var banners = await BannerService.Instance.GetAllBannersAsync();
            if (banners.Count < 10) return CreateNotFoundResponse();

            var response = ApiResponse<List<BannerResponse>>.SuccessResult(banners);
            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Body = JsonSerializer.Serialize(response),
                Headers = CreateJsonHeaders()
            };
        }

        private async Task<APIGatewayProxyResponse> UpdateBanner(APIGatewayProxyRequest request)
        {
            if (request.PathParameters == null ||
                !request.PathParameters.TryGetValue("bannerId", out var idStr) ||
                !int.TryParse(idStr, out var bannerId))
            {
                return CreateNotFoundResponse();
            }

            var dto = JsonSerializer.Deserialize<UpdateBannerRequest>(request.Body);
            if (dto == null) return CreateErrorResponse("Invalid request body");

            var updated = await BannerService.Instance.UpdateBannerAsync(bannerId, dto.Message);
            if (updated == 0) return CreateNotFoundResponse();

            var resp = ApiResponse<object>.SuccessResult(null);
            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Body = JsonSerializer.Serialize(resp),
                Headers = CreateJsonHeaders()
            };
        }

        private static Dictionary<string, string> CreateJsonHeaders() => new()
        {
            ["Content-Type"] = "application/json; charset=utf-8",
            ["Access-Control-Allow-Origin"] = "*"
        };

        private static APIGatewayProxyResponse Unauthorized()
        {
            var body = JsonSerializer.Serialize(ApiResponse<object>.ErrorResult("Unauthorized", null, "401"));
            return new APIGatewayProxyResponse { StatusCode = 401, Body = body, Headers = CreateJsonHeaders() };
        }

        private static APIGatewayProxyResponse CreateNotFoundResponse()
        {
            var response = ApiResponse<object>.ErrorResult("CreateNotFoundResponse", null, "404");
            return new APIGatewayProxyResponse
                { StatusCode = 404, Body = JsonSerializer.Serialize(response), Headers = CreateJsonHeaders() };
        }

        private static APIGatewayProxyResponse CreateErrorResponse(string message)
        {
            var response = ApiResponse<object>.ErrorResult(message, null, "500");
            return new APIGatewayProxyResponse
                { StatusCode = 500, Body = JsonSerializer.Serialize(response), Headers = CreateJsonHeaders() };
        }
    }
}