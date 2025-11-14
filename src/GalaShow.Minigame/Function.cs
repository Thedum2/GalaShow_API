using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using GalaShow.Common;
using GalaShow.Common.Cors;
using GalaShow.Common.Errors;
using GalaShow.Common.Infrastructure;
using GalaShow.Common.Models;
using GalaShow.Common.Models.Request.Minigame;
using GalaShow.Common.Models.Response.Minigame;
using GalaShow.Common.Service;
using Microsoft.IdentityModel.Tokens;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace GalaShow.Minigame
{
    public class Function
    {
        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest req, ILambdaContext context)
        {
            StageResolver.Resolve(req);
            await AppBootstrap.InitAsync();

            APIGatewayProxyResponse response;
            try
            {
                response = (req.HttpMethod, req.Path) switch
                {
                    // [6-1] 미니게임 목록 조회
                    ("GET", "/minigames") => await TokenService.Instance.RequireAuthThen(
                        req,
                        _ => GetMinigames(req),
                        () => ErrorResults.Json(ErrorCode.AuthTokenExpired),
                        () => ErrorResults.Json(ErrorCode.Unauthorized)
                    ),

                    // [6-2] 미니게임 생성
                    ("POST", "/minigames") => await TokenService.Instance.RequireAuthThen(
                        req,
                        _ => CreateMinigame(req),
                        () => ErrorResults.Json(ErrorCode.AuthTokenExpired),
                        () => ErrorResults.Json(ErrorCode.Unauthorized)
                    ),

                    // [6-3] 미니게임 상세 조회
                    ("GET", var p) when p.StartsWith("/minigames/") && p.EndsWith("/survival-rate") == false =>
                        await TokenService.Instance.RequireAuthThen(
                            req,
                            _ => GetMinigameDetail(req),
                            () => ErrorResults.Json(ErrorCode.AuthTokenExpired),
                            () => ErrorResults.Json(ErrorCode.Unauthorized)
                        ),

                    // [6-4] 미니게임 수정
                    ("PUT", var p) when p.StartsWith("/minigames/") && p.Contains("/survival-rate") == false =>
                        await TokenService.Instance.RequireAuthThen(
                            req,
                            _ => UpdateMinigame(req),
                            () => ErrorResults.Json(ErrorCode.AuthTokenExpired),
                            () => ErrorResults.Json(ErrorCode.Unauthorized)
                        ),

                    // [6-5] 미니게임 삭제
                    ("DELETE", var p) when p.StartsWith("/minigames/") =>
                        await TokenService.Instance.RequireAuthThen(
                            req,
                            _ => DeleteMinigame(req),
                            () => ErrorResults.Json(ErrorCode.AuthTokenExpired),
                            () => ErrorResults.Json(ErrorCode.Unauthorized)
                        ),

                    // [6-6] 미니게임 생존률 조회
                    ("GET", var p) when p.Contains("/survival-rate") =>
                        await TokenService.Instance.RequireAuthThen(
                            req,
                            _ => GetSurvivalRate(req),
                            () => ErrorResults.Json(ErrorCode.AuthTokenExpired),
                            () => ErrorResults.Json(ErrorCode.Unauthorized)
                        ),

                    // [6-7] 미니게임 생존률 데이터 추가
                    ("POST", var p) when p.Contains("/survival-rate") =>
                        await TokenService.Instance.RequireAuthThen(
                            req,
                            _ => AddSurvivalRate(req),
                            () => ErrorResults.Json(ErrorCode.AuthTokenExpired),
                            () => ErrorResults.Json(ErrorCode.Unauthorized)
                        ),

                    // [6-8] 미니게임 생존률 수정 (관리자)
                    ("PUT", var p) when p.Contains("/survival-rate") =>
                        await TokenService.Instance.RequireAuthThen(
                            req,
                            _ => UpdateSurvivalRate(req),
                            () => ErrorResults.Json(ErrorCode.AuthTokenExpired),
                            () => ErrorResults.Json(ErrorCode.Unauthorized)
                        ),

                    // [6-9] 시청자 아바타 목록 조회
                    ("GET", "/viewer-avatars") => await TokenService.Instance.RequireAuthThen(
                        req,
                        _ => GetViewerAvatars(),
                        () => ErrorResults.Json(ErrorCode.AuthTokenExpired),
                        () => ErrorResults.Json(ErrorCode.Unauthorized)
                    ),

                    // [6-10] 시청자 아바타 목록 수정
                    ("PUT", "/viewer-avatars") => await TokenService.Instance.RequireAuthThen(
                        req,
                        _ => UpdateViewerAvatars(req),
                        () => ErrorResults.Json(ErrorCode.AuthTokenExpired),
                        () => ErrorResults.Json(ErrorCode.Unauthorized)
                    ),

                    ("OPTIONS", _) => Success200(),

                    _ => ErrorResults.Json(ErrorCode.Forbidden)
                };
            }
            catch (SecurityTokenException ste)
            {
                context.Logger.LogError($"Auth error: {ste.Message}");
                response = ErrorResults.Json(ErrorCode.Unauthorized);
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Error: {ex}");
                response = ErrorResults.Json(ErrorCode.Internal);
            }

            return CorsHandler.AddCorsHeaders(req, response);
        }

        #region ==================== 미니게임 관리 Handlers ====================

        private async Task<APIGatewayProxyResponse> GetMinigames(APIGatewayProxyRequest request)
        {
            var queryParams = request.QueryStringParameters ?? new Dictionary<string, string>();

            var scale = queryParams.TryGetValue("scale", out var s) ? s : null;
            var difficulty = queryParams.TryGetValue("difficulty", out var d) ? d : null;
            var round = queryParams.TryGetValue("round", out var r) ? r : null;
            var type = queryParams.TryGetValue("type", out var t) ? t : null;
            var survivalRate = queryParams.TryGetValue("survivalRate", out var sr) ? sr : null;
            var winCondition = queryParams.TryGetValue("winCondition", out var wc) ? wc : null;

            var response = await MinigameService.Instance.GetAllMinigamesAsync(
                scale, difficulty, round, type, survivalRate, winCondition);

            return Success200(response);
        }

        private async Task<APIGatewayProxyResponse> GetMinigameDetail(APIGatewayProxyRequest request)
        {
            if (request.PathParameters == null || !request.PathParameters.TryGetValue("gameId", out var idStr) || !int.TryParse(idStr, out var gameId))
            {
                return ErrorResults.Json(ErrorCode.BadRequest, "Invalid gameId");
            }

            var response = await MinigameService.Instance.GetMinigameDetailAsync(gameId);
            if (response == null)
            {
                return ErrorResults.Json(ErrorCode.MinigameNotFound);
            }

            return Success200(response);
        }

        private async Task<APIGatewayProxyResponse> CreateMinigame(APIGatewayProxyRequest request)
        {
            var dto = JsonSerializer.Deserialize<CreateMinigameRequest>(request.Body);
            if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
            {
                return ErrorResults.Json(ErrorCode.BadRequest, "Invalid request body");
            }

            if (await MinigameService.Instance.ExistsByNameAsync(dto.Name))
            {
                return ErrorResults.Json(ErrorCode.MinigameAlreadyExists);
            }

            try
            {
                var response = await MinigameService.Instance.CreateMinigameAsync(dto);
                if (response == null)
                {
                    return ErrorResults.Json(ErrorCode.MinigameCreateFailed);
                }

                return Success200(response);
            }
            catch (Exception)
            {
                return ErrorResults.Json(ErrorCode.MinigameCreateFailed);
            }
        }

        private async Task<APIGatewayProxyResponse> UpdateMinigame(APIGatewayProxyRequest request)
        {
            if (request.PathParameters == null || !request.PathParameters.TryGetValue("gameId", out var idStr) || !int.TryParse(idStr, out var gameId))
            {
                return ErrorResults.Json(ErrorCode.BadRequest, "Invalid gameId");
            }

            var dto = JsonSerializer.Deserialize<UpdateMinigameRequest>(request.Body);
            if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
            {
                return ErrorResults.Json(ErrorCode.BadRequest, "Invalid request body");
            }

            var existing = await MinigameService.Instance.ExistsByIdAsync(gameId);
            if (!existing)
            {
                return ErrorResults.Json(ErrorCode.MinigameNotFound);
            }

            if (await MinigameService.Instance.ExistsByNameAsync(dto.Name, gameId))
            {
                return ErrorResults.Json(ErrorCode.MinigameAlreadyExists);
            }

            try
            {
                var response = await MinigameService.Instance.UpdateMinigameAsync(gameId, dto);
                if (response == null)
                {
                    return ErrorResults.Json(ErrorCode.MinigameUpdateFailed);
                }

                return Success200(response);
            }
            catch (Exception)
            {
                return ErrorResults.Json(ErrorCode.MinigameUpdateFailed);
            }
        }

        private async Task<APIGatewayProxyResponse> DeleteMinigame(APIGatewayProxyRequest request)
        {
            if (request.PathParameters == null || !request.PathParameters.TryGetValue("gameId", out var idStr) || !int.TryParse(idStr, out var gameId))
            {
                return ErrorResults.Json(ErrorCode.BadRequest, "Invalid gameId");
            }

            var existing = await MinigameService.Instance.ExistsByIdAsync(gameId);
            if (!existing)
            {
                return ErrorResults.Json(ErrorCode.MinigameNotFound);
            }

            var deleted = await MinigameService.Instance.DeleteMinigameAsync(gameId);
            if (deleted == 0)
            {
                return ErrorResults.Json(ErrorCode.MinigameNotFound);
            }

            return Success200();
        }

        #endregion

        #region ==================== 생존률 관리 Handlers ====================

        private async Task<APIGatewayProxyResponse> GetSurvivalRate(APIGatewayProxyRequest request)
        {
            if (request.PathParameters == null || !request.PathParameters.TryGetValue("gameId", out var idStr) || !int.TryParse(idStr, out var gameId))
            {
                return ErrorResults.Json(ErrorCode.BadRequest, "Invalid gameId");
            }

            var response = await MinigameService.Instance.GetSurvivalRateAsync(gameId);
            if (response == null)
            {
                return ErrorResults.Json(ErrorCode.MinigameNotFound);
            }

            return Success200(response);
        }

        private async Task<APIGatewayProxyResponse> AddSurvivalRate(APIGatewayProxyRequest request)
        {
            if (request.PathParameters == null || !request.PathParameters.TryGetValue("gameId", out var idStr) || !int.TryParse(idStr, out var gameId))
            {
                return ErrorResults.Json(ErrorCode.BadRequest, "Invalid gameId");
            }

            var dto = JsonSerializer.Deserialize<AddSurvivalRateRequest>(request.Body);
            if (dto == null || dto.TotalPlayers <= 0 || dto.Survivors < 0 || dto.Survivors > dto.TotalPlayers)
            {
                return ErrorResults.Json(ErrorCode.BadRequest, "Invalid request body");
            }

            var response = await MinigameService.Instance.AddSurvivalRateAsync(gameId, dto.TotalPlayers, dto.Survivors);
            if (response == null)
            {
                return ErrorResults.Json(ErrorCode.MinigameNotFound);
            }

            return Success200(response);
        }

        private async Task<APIGatewayProxyResponse> UpdateSurvivalRate(APIGatewayProxyRequest request)
        {
            if (request.PathParameters == null || !request.PathParameters.TryGetValue("gameId", out var idStr) || !int.TryParse(idStr, out var gameId))
            {
                return ErrorResults.Json(ErrorCode.BadRequest, "Invalid gameId");
            }

            var dto = JsonSerializer.Deserialize<UpdateSurvivalRateRequest>(request.Body);
            if (dto == null || dto.TotalGames < 0 || dto.TotalPlayers < 0 || dto.Survivors < 0)
            {
                return ErrorResults.Json(ErrorCode.BadRequest, "Invalid request body");
            }

            var response = await MinigameService.Instance.UpdateSurvivalRateAsync(
                gameId, dto.SurvivalRate, dto.TotalGames, dto.TotalPlayers, dto.Survivors);

            if (response == null)
            {
                return ErrorResults.Json(ErrorCode.MinigameNotFound);
            }

            return Success200(response);
        }

        #endregion

        #region ==================== 시청자 아바타 Handlers ====================

        private async Task<APIGatewayProxyResponse> GetViewerAvatars()
        {
            var response = await ViewerAvatarService.Instance.GetAllAvatarsAsync();
            return Success200(response);
        }

        private async Task<APIGatewayProxyResponse> UpdateViewerAvatars(APIGatewayProxyRequest request)
        {
            var dto = JsonSerializer.Deserialize<UpdateViewerAvatarsRequest>(request.Body);
            if (dto == null || dto.Data == null || dto.Data.Count == 0)
            {
                return ErrorResults.Json(ErrorCode.BadRequest, "Invalid request body");
            }

            if (await ViewerAvatarService.Instance.HasDuplicateOrdersAsync(dto.Data))
            {
                return ErrorResults.Json(ErrorCode.AvatarDuplicateOrder);
            }

            try
            {
                await ViewerAvatarService.Instance.UpdateAllAvatarsAsync(dto.Data);
                return Success200();
            }
            catch (Exception)
            {
                return ErrorResults.Json(ErrorCode.AvatarUpdateFailed);
            }
        }

        #endregion

        #region ==================== Helpers ====================

        private static Dictionary<string, string> JsonHeaders() => ResponseHeaders.Get();

        private static APIGatewayProxyResponse Success200<T>(T body) => new()
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
