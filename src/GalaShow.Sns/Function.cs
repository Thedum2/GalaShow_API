#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using GalaShow.Common;
using GalaShow.Common.Cors;
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
        public async Task<APIGatewayProxyResponse?> FunctionHandler(APIGatewayProxyRequest request,
            ILambdaContext context)
        {
            StageResolver.Resolve(request);
            await AppBootstrap.InitAsync();

            APIGatewayProxyResponse? response;
            try
            {
                response = (request.HttpMethod, request.Path) switch
                {
                    ("GET", "/sns-links") => await GetSnsLinks(),
                    ("PUT", "/sns-links") =>
                        await TokenService.Instance.RequireAuthThen(
                            request,
                            _ => UpdateSnsLinks(request),
                            () => ErrorResults.Json(ErrorCode.AuthTokenExpired),
                            () => ErrorResults.Json(ErrorCode.Unauthorized)
                        ),

                    ("OPTIONS", _) => Success200(),

                    _ => ErrorResults.Json(ErrorCode.PathNotFound)
                };
            }
            catch (SecurityTokenException ste)
            {
                context.Logger.LogError($"Auth error: {ste.Message}");
                response = ErrorResults.Json(ErrorCode.Unauthorized);
            }
            catch (Exception ex)
            {
                context.Logger.LogError(ex.ToString());
                response = ErrorResults.Json(ErrorCode.Internal);
            }

            if (response is null)
            {
                context.Logger.LogError("Response was unexpectedly null.");
                response = ErrorResults.Json(ErrorCode.Internal, "An unexpected error occurred where the response was null.");
            }

            return CorsHandler.AddCorsHeaders(request, response);
        }


        #region !============================ Handlers ============================!

        private static async Task<APIGatewayProxyResponse?> GetSnsLinks()
        {
            var list = await SnsService.Instance.GetAllAsync();
            return Success200(list);
        }

        private static async Task<APIGatewayProxyResponse?> UpdateSnsLinks(APIGatewayProxyRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Body))
                return ErrorResults.Json(ErrorCode.SnsLinkNotFound);

            var dto = JsonSerializer.Deserialize<UpdateSnsLinksRequest>(req.Body);
            if (dto is null || dto.Data.Count == 0)
                return ErrorResults.Json(ErrorCode.SnsLinkUpdateFailed);

            await SnsService.Instance.ReplaceAllAsync(dto);
            return Success200();
        }

        #endregion

        #region !============================ Helpers ============================!

        private static Dictionary<string, string> JsonHeaders() => ResponseHeaders.Get();

        private static APIGatewayProxyResponse? Success200<T>(T? body) => new()
        {
            StatusCode = 200,
            Headers = JsonHeaders(),
            Body = JsonSerializer.Serialize(ApiResponse<T>.Success(body))
        };

        private static APIGatewayProxyResponse Success200() => new()
        {
            StatusCode = 200,
            Headers = JsonHeaders(),
            Body = JsonSerializer.Serialize(ApiResponse<object>.Success())
        };

        #endregion
    }
}
