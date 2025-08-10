#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Security.Claims;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using GalaShow.Common.Configuration;
using GalaShow.Common.Models;
using GalaShow.Common.Models.Request;
using GalaShow.Common.Models.Response;
using GalaShow.Common.Repositories;
using GalaShow.Common.Service;
using GalaShow.Common.Auth;
using Microsoft.IdentityModel.Tokens;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace GalaShow.Banner
{
    public class Function
    {
        private static readonly object Lock = new();
        private static DatabaseConfig? _dbConfig;
        private static IJwtValidator? _jwtValidator;

        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            try
            {
                EnsureDbConfig();
                await EnsureJwtAsync();

                var secretsHelper = new SecretsManagerHelper();
                var credentials = await secretsHelper.GetDbCredentialsAsync(_dbConfig!.SecretArn);

                using var dbService = new DatabaseService(_dbConfig!, (credentials.Username, credentials.Password));
                await dbService.OpenAsync();

                var bannerService = new BannerService(new BannerRepository(dbService));

                return (request.HttpMethod, request.Path) switch
                {
                    ("GET", "/banners") => await GetAllBanners(bannerService),
                    ("PUT", var p) when p.StartsWith("/banners/") =>
                        await RequireAuthThen(request, async _ => await UpdateBanner(bannerService, request)),

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

        private void EnsureDbConfig()
        {
            if (_dbConfig != null) return;
            lock (Lock) { _dbConfig ??= new DatabaseConfig(); }
        }

        private async Task EnsureJwtAsync()
        {
            if (_jwtValidator != null) return;
            lock (Lock)
            {
                _jwtValidator ??= new JwtValidator(JwtOptions.FromEnv());
            }
            await _jwtValidator.InitializeAsync();
        }

        private async Task<APIGatewayProxyResponse> RequireAuthThen(APIGatewayProxyRequest req, Func<ClaimsPrincipal, Task<APIGatewayProxyResponse>> next)
        {
            var auth = req.Headers != null && req.Headers.TryGetValue("Authorization", out var v) ? v : null;
            var user = _jwtValidator!.ValidateBearer(auth);
            return await next(user);
        }

        private async Task<APIGatewayProxyResponse> GetAllBanners(BannerService bannerService)
        {
            var banners = await bannerService.GetAllBannersAsync();
            if (banners.Count < 10) return CreateNotFoundResponse();

            var response = ApiResponse<List<BannerResponse>>.SuccessResult(banners);
            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Body = JsonSerializer.Serialize(response),
                Headers = CreateJsonHeaders()
            };
        }

        private async Task<APIGatewayProxyResponse> UpdateBanner(BannerService bannerService, APIGatewayProxyRequest request)
        {
            if (request.PathParameters == null ||
                !request.PathParameters.TryGetValue("bannerId", out var idStr) ||
                !int.TryParse(idStr, out var bannerId))
            {
                return CreateNotFoundResponse();
            }

            var dto = JsonSerializer.Deserialize<UpdateBannerRequest>(request.Body);
            if (dto == null) return CreateErrorResponse("Invalid request body");

            var updated = await bannerService.UpdateBannerAsync(bannerId, dto.Message);
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

        private static APIGatewayProxyResponse Forbidden()
        {
            var body = JsonSerializer.Serialize(ApiResponse<object>.ErrorResult("Forbidden", null, "403"));
            return new APIGatewayProxyResponse { StatusCode = 403, Body = body, Headers = CreateJsonHeaders() };
        }

        private static APIGatewayProxyResponse CreateNotFoundResponse()
        {
            var response = ApiResponse<object>.ErrorResult("CreateNotFoundResponse", null, "404");
            return new APIGatewayProxyResponse { StatusCode = 404, Body = JsonSerializer.Serialize(response), Headers = CreateJsonHeaders() };
        }

        private static APIGatewayProxyResponse CreateErrorResponse(string message)
        {
            var response = ApiResponse<object>.ErrorResult(message, null, "500");
            return new APIGatewayProxyResponse { StatusCode = 500, Body = JsonSerializer.Serialize(response), Headers = CreateJsonHeaders() };
        }
    }
}
