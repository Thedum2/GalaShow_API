using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using GalaShow.Common.Errors;
using GalaShow.Common.Infrastructure;
using GalaShow.Common.Models;
using GalaShow.Common.Models.Request.Background;
using GalaShow.Common.Models.Request.Banner;
using GalaShow.Common.Models.Response.Background;
using GalaShow.Common.Service;
using Microsoft.IdentityModel.Tokens;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace GalaShow.BackGround
{
    public class Function
    {
        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest req, ILambdaContext context)
        {
            StageResolver.Resolve(req);
            await AppBootstrap.InitAsync();

            try
            {
                return (req.HttpMethod, req.Path) switch
                {
                    ("GET", "/background") => await GetAllBackground(),

                    ("PUT", var p) when p.StartsWith("/background/") =>
                        await TokenService.Instance.RequireAuthThen(
                            req,
                            _ => UpdateBackground(req),
                            () => ErrorResults.Json(ErrorCode.AuthTokenExpired),
                            () => ErrorResults.Json(ErrorCode.Unauthorized)
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

        private async Task<APIGatewayProxyResponse> GetAllBackground()
        {
            var background = await BackgroundService.Instance.GetAllBackgroundAsync();
            if (background.Count < 3)
            {
                return ErrorResults.Json(ErrorCode.BackgroundNotFound);
            }
            return Success200(background);
        }

        private async Task<APIGatewayProxyResponse> UpdateBackground(APIGatewayProxyRequest request)
        {
            if (request.PathParameters == null || !request.PathParameters.TryGetValue("backId", out var idStr) ||
                !int.TryParse(idStr, out var backId))
            {
                return ErrorResults.Json(ErrorCode.BackgroundNotFound);
            }

            var dto = JsonSerializer.Deserialize<UpdateBackgroundRequest>(request.Body);
            if (dto is null || string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Url))
            {
                return ErrorResults.Json(ErrorCode.BadRequest, "Invalid request body");
            }

            var updated = await BackgroundService.Instance.UpdateBannerAsync(backId, dto.Title, dto.Type, dto.Url);
            if (updated == 0)
            {
                return ErrorResults.Json(ErrorCode.BackgroundUpdateFailed);
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