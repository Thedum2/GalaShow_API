using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using GalaShow.Common.Errors;
using GalaShow.Common.Infrastructure;
using GalaShow.Common.Models;
using GalaShow.Common.Models.Request.Sns;
using GalaShow.Common.Service;
using Microsoft.IdentityModel.Tokens;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace GalaShow.Sns
{
    public class Function
    {
        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request,
            ILambdaContext context)
        {
            StageResolver.Resolve(request);
            await AppBootstrap.InitAsync();

            try
            {
                return (request.HttpMethod, request.Path) switch
                {
                    ("GET", "/sns-links") => await GetSnsLinks(),
                    ("PUT", "/sns-links") =>
                        await TokenService.Instance.RequireAuthThen(
                            request,
                            _ => UpdateSnsLinks(request),
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
                context.Logger.LogError(ex.ToString());
                return ErrorResults.Json(ErrorCode.Internal);
            }
        }


        #region !============================Handlers============================!

        private static async Task<APIGatewayProxyResponse> GetSnsLinks()
        {
            var list = await SnsService.Instance.GetAllAsync();
            return Json200(list);
        }

        private static async Task<APIGatewayProxyResponse> UpdateSnsLinks(APIGatewayProxyRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Body))
                return ErrorResults.Json(ErrorCode.SnsLinkNotFound);

            var dto = JsonSerializer.Deserialize<UpdateSnsLinksRequest>(req.Body);
            if (dto is null || dto.Data.Count == 0)
                return ErrorResults.Json(ErrorCode.SnsLinkUpdateFailed);

            await SnsService.Instance.ReplaceAllAsync(dto);
            return Json200<object?>(null);
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

        private static APIGatewayProxyResponse Json200<T>(T body) => new()
        {
            StatusCode = 200,
            Headers = JsonHeaders(),
            Body = JsonSerializer.Serialize(ApiResponse<T>.SuccessResult(body))
        };

        #endregion
    }
}