using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using GalaShow.Common.Errors;
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

                    ("PUT", var p) when p.StartsWith("/banners/") =>
                        await TokenService.Instance.RequireAuthThen(
                            request,
                            _ => UpdateBanner(request),
                            ()=> ErrorResults.Json(ErrorCode.AuthTokenExpired),
                            ()=> ErrorResults.Json(ErrorCode.Unauthorized)
                        ),

                    _ => ErrorResults.Json(ErrorCode.PathNotFound)
                };
            }
            catch (SecurityTokenException ste)
            {
                context.Logger.LogError($"Auth error: {ste.Message}");
                return ErrorResults.Json(ErrorCode.Unauthorized);
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Error: {ex}");
                return ErrorResults.Json(ErrorCode.Internal);
            }
        }

        #region !============================Handlers============================!

        private async Task<APIGatewayProxyResponse> GetAllBanners()
        {
            var banners = await BannerService.Instance.GetAllBannersAsync();
            if (banners.Count < 10)
            {
                return ErrorResults.Json(ErrorCode.BannerNotFound);
            }

            var response = ApiResponse<List<BannerResponse>>.SuccessResult(banners);
            return Success200(response);
        }

        private async Task<APIGatewayProxyResponse> UpdateBanner(APIGatewayProxyRequest request)
        {
            if (request.PathParameters == null || !request.PathParameters.TryGetValue("bannerId", out var idStr) || !int.TryParse(idStr, out var bannerId))
            {
                return ErrorResults.Json(ErrorCode.BannerUpdateFailed);
            }

            var dto = JsonSerializer.Deserialize<UpdateBannerRequest>(request.Body);
            if (dto == null)
            {
                return ErrorResults.Json(ErrorCode.BadRequest, "Invalid request body");
            }

            var updated = await BannerService.Instance.UpdateBannerAsync(bannerId, dto.Message);
            if (updated == 0)
            {
                return ErrorResults.Json(ErrorCode.BannerUpdateFailed);
            }

            return Success200<object>(null);
        }
        #endregion

        #region !============================Helpers============================!

        private static Dictionary<string, string> JsonHeaders() => new()
        {
            ["Content-Type"] = "application/json; charset=utf-8",
            ["Access-Control-Allow-Origin"] = "*"
        };
        
        #endregion
        
        #region !============================Helpers(Only Success(200))============================!

        private static APIGatewayProxyResponse Success200<T>(T body) => new()
        {
            StatusCode = 200,
            Headers = JsonHeaders(),
            Body = JsonSerializer.Serialize(ApiResponse<T>.SuccessResult(body))
        };

        #endregion
    }
}