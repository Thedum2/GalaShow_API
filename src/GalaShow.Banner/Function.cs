using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using GalaShow.Common.Configuration;
using GalaShow.Common.Models;
using GalaShow.Common.Models.Request;
using GalaShow.Common.Models.Response;
using GalaShow.Common.Repositories;
using GalaShow.Common.Service;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace GalaShow.Banner
{
    public class Function
    {
        private static DatabaseConfig? _dbConfig;
        private static readonly object _lock = new object();

        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request,
            ILambdaContext context)
        {
            try
            {
                if (_dbConfig == null)
                {
                    lock (_lock)
                    {
                        _dbConfig ??= new DatabaseConfig();
                    }
                }

                var secretsHelper = new SecretsManagerHelper();
                var credentials = await secretsHelper.GetDbCredentialsAsync(_dbConfig.SecretArn);

                using var dbService = new DatabaseService(_dbConfig, (credentials.Username, credentials.Password));
                await dbService.OpenAsync();

                var bannerRepository = new BannerRepository(dbService);
                var bannerService = new BannerService(bannerRepository);

                var path = request.Path;
                var method = request.HttpMethod;

                return (method, path) switch
                {
                    ("GET", "/banners") => await GetAllBanners(bannerService),
                    ("PUT", var p) when p.StartsWith("/banners/") => await UpdateBanner(bannerService, request),
                    _ => CreateNotFoundResponse()
                };
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Error: {ex.Message}");
                return CreateErrorResponse("Internal server error");
            }
        }

        private async Task<APIGatewayProxyResponse> GetAllBanners(BannerService bannerService)
        {
            var banners = await bannerService.GetAllBannersAsync();

            if (banners.Count < 10)
            {
                return CreateNotFoundResponse();
            }

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
            var segments = request.Path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (request.PathParameters == null || !request.PathParameters.TryGetValue("bannerId", out var idStr)
                || !int.TryParse(idStr, out var bannerId))
            {
                return CreateNotFoundResponse();
            }

            var dto = JsonSerializer.Deserialize<UpdateBannerRequest>(request.Body);
            if (dto == null)
            {
                return CreateErrorResponse("Invalid request body");
            }

            var updated = await bannerService.UpdateBannerAsync(bannerId, dto.Message);
            if (updated == 0)
            {
                return CreateNotFoundResponse();
            }
            
            var resp = ApiResponse<object>.SuccessResult(null);
            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Body = JsonSerializer.Serialize(resp),
                Headers = CreateJsonHeaders()
            };
        }

        private static Dictionary<string, string> CreateJsonHeaders()
        {
            return new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json; charset=utf-8",
                ["Access-Control-Allow-Origin"] = "*"
            };
        }

        private static APIGatewayProxyResponse CreateNotFoundResponse()
        {
            var response = ApiResponse<List<BannerResponse>>.ErrorResult("CreateNotFoundResponse", null, "404");
            return new APIGatewayProxyResponse
            {
                StatusCode = 404,
                Body = JsonSerializer.Serialize(response),
                Headers = CreateJsonHeaders()
            };
        }

        private static APIGatewayProxyResponse CreateErrorResponse(string message)
        {
            var response = ApiResponse<List<BannerResponse>>.ErrorResult(message, null, "500");
            return new APIGatewayProxyResponse
            {
                StatusCode = 500,
                Body = JsonSerializer.Serialize(response),
                Headers = CreateJsonHeaders()
            };
        }
    }
}