#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using GalaShow.Common.Errors;
using GalaShow.Common.Infrastructure;
using GalaShow.Common.Models;
using GalaShow.Common.Models.Request.Policy;
using GalaShow.Common.Models.Response.Policy;
using GalaShow.Common.Service;
using Microsoft.IdentityModel.Tokens;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace GalaShow.Policy
{
    public class Function
    {
        public async Task<APIGatewayProxyResponse?> FunctionHandler(APIGatewayProxyRequest req,
            ILambdaContext context)
        {
            StageResolver.Resolve(req);
            await AppBootstrap.InitAsync();

            try
            {
                return (req.HttpMethod, req.Path) switch
                {
                    ("GET", "/policies") => await GetPolicy(),

                    ("PUT", "/policies") =>
                        await TokenService.Instance.RequireAuthThen(
                            req,
                            _ => UpdatePolicy(req),
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

        private static async Task<APIGatewayProxyResponse?> GetPolicy()
        {
            var data = await PolicyService.Instance.GetAsync();
            return Json200(data);
        }

        private static async Task<APIGatewayProxyResponse?> UpdatePolicy(APIGatewayProxyRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Body))
            {
                return ErrorResults.Json(ErrorCode.PolicyNotFound);
            }

            var dto = JsonSerializer.Deserialize<UpdatePolicyRequest>(req.Body);
            if (dto is null)
            {
                return ErrorResults.Json(ErrorCode.PolicyNotFound);
            }

            var tos = dto.TermsOfService?.Trim() ?? "";
            var pp = dto.PrivacyPolicy?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(tos) || string.IsNullOrWhiteSpace(pp))
            {
                return ErrorResults.Json(ErrorCode.PolicyNotFound);
            }

            await PolicyService.Instance.UpdateAsync(tos, pp);
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
        private static APIGatewayProxyResponse? Json200<T>(T body) => new()
        {
            StatusCode = 200,
            Headers = JsonHeaders(),
            Body = JsonSerializer.Serialize(ApiResponse<T>.SuccessResult(body))
        };
        
        #endregion
    }
}