using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using GalaShow.Common;
using GalaShow.Common.Auth;
using GalaShow.Common.Cors;
using GalaShow.Common.Errors;
using GalaShow.Common.Infrastructure;
using GalaShow.Common.Models;
using GalaShow.Common.Models.Request.Token;
using GalaShow.Common.Models.Response.Token;
using GalaShow.Common.Repositories;
using GalaShow.Common.Service;
using Microsoft.IdentityModel.Tokens;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace GalaShow.Token
{
    public class Function
    {
        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest req, ILambdaContext ctx)
        {
            StageResolver.Resolve(req);
            await AppBootstrap.InitAsync();

            APIGatewayProxyResponse response;
            try
            {
                response = (req.HttpMethod, req.Path) switch
                {
                    ("POST", "/auth/login") => await Login(req),
                    ("POST", "/auth/refresh") => await Refresh(req),
                    ("POST", "/auth/logout") => await Logout(req),
                    ("GET", "/auth/verify") => await Verify(req),

                    ("OPTIONS", _) => Success200(),

                    _ => ErrorResults.Json(ErrorCode.PathNotFound)
                };
            }
            catch (SecurityTokenException ste)
            {
                ctx.Logger.LogError($"Auth error: {ste.Message}");
                response = ErrorResults.Json(ErrorCode.Unauthorized);
            }
            catch (Exception ex)
            {
                ctx.Logger.LogError(ex.ToString());
                response = ErrorResults.Json(ErrorCode.Internal);
            }

            return CorsHandler.AddCorsHeaders(req, response);
        }

        #region !============================ Handlers ============================!

        private static async Task<APIGatewayProxyResponse> Login(APIGatewayProxyRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Body)) return ErrorResults.Json(ErrorCode.BadRequest);

            var dto = JsonSerializer.Deserialize<LoginRequest>(req.Body);
            if (dto is null || string.IsNullOrWhiteSpace(dto.Id) || string.IsNullOrWhiteSpace(dto.Password))
                return ErrorResults.Json(ErrorCode.BadRequest);

            var (ok, role) = await TokenService.Instance.ValidateCredentialAsync(dto.Id, dto.Password);
            if (!ok) return ErrorResults.Json(ErrorCode.AuthInvalidCredentials);

            var access = TokenService.Instance.IssueAccessToken(dto.Id, role);
            var (raw, hash, refreshExpUtc) = TokenService.Instance.CreateRefreshToken();

            var (ua, ip) = GetUaAndIp(req);
            var repo = new TokenRepository();
            await repo.InsertAsync(dto.Id, hash, refreshExpUtc, ua, ip);

            var handler = new JwtSecurityTokenHandler();
            var parsed = handler.ReadJwtToken(access);
            var accessExpUtc = parsed.ValidTo.ToUniversalTime();
            var accessExpiresIn = (int)Math.Max(0, (accessExpUtc - DateTime.UtcNow).TotalSeconds);
            var refreshExpiresIn = (int)Math.Max(0, (refreshExpUtc - DateTime.UtcNow).TotalSeconds);

            var resp = new LoginResponse.TokenBundleResponse
            {
                AccessToken = access,
                ExpiresIn = accessExpiresIn,
                AccessExpiresAt = accessExpUtc,

                RefreshToken = raw,
                RefreshExpiresIn = refreshExpiresIn,
                RefreshExpiresAt = refreshExpUtc,

                User = new LoginResponse.UserPayload { Id = dto.Id, Role = role }
            };
            return Success200(resp);
        }

        private static async Task<APIGatewayProxyResponse> Refresh(APIGatewayProxyRequest req)
        {
            Console.WriteLine("[Refresh] Starting refresh token flow");

            if (string.IsNullOrWhiteSpace(req.Body))
            {
                Console.WriteLine("[Refresh] Request body is empty");
                return ErrorResults.Json(ErrorCode.Unauthorized);
            }

            Console.WriteLine($"[Refresh] Request body: {req.Body}");

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var dto = JsonSerializer.Deserialize<RefreshRequest>(req.Body, options);

            Console.WriteLine($"[Refresh] Deserialized DTO - RefreshToken: {dto?.RefreshToken ?? "null"}");

            if (string.IsNullOrWhiteSpace(dto?.RefreshToken))
            {
                Console.WriteLine("[Refresh] RefreshToken is null or empty");
                return ErrorResults.Json(ErrorCode.Unauthorized);
            }

            Console.WriteLine($"[Refresh] Received refresh token (length: {dto.RefreshToken.Length})");
            var hash = TokenService.HashRefreshRaw(dto.RefreshToken);
            Console.WriteLine($"[Refresh] Token hash: {hash}");

            var repo = new TokenRepository();
            var rec = await repo.GetByHashAsync(hash);

            if (rec is null)
            {
                Console.WriteLine("[Refresh] Token not found in database");
                return ErrorResults.Json(ErrorCode.AuthRefreshInvalid);
            }

            Console.WriteLine($"[Refresh] Token found - UserId: {rec.UserId}, ExpiresAt: {rec.ExpiresAt:O}, RevokedAt: {rec.RevokedAt?.ToString("O") ?? "null"}");

            if (rec.RevokedAt.HasValue)
            {
                Console.WriteLine("[Refresh] Token already revoked");
                return ErrorResults.Json(ErrorCode.AuthRefreshRevoked);
            }

            if (rec.ExpiresAt <= DateTime.UtcNow)
            {
                Console.WriteLine($"[Refresh] Token expired. ExpiresAt: {rec.ExpiresAt:O}, Now: {DateTime.UtcNow:O}");
                return ErrorResults.Json(ErrorCode.AuthRefreshExpired);
            }

            Console.WriteLine("[Refresh] Revoking old token");
            await repo.RevokeAsync(hash);

            Console.WriteLine("[Refresh] Resolving user role");
            var role = await ResolveRoleAsync(rec.UserId);
            Console.WriteLine($"[Refresh] Role resolved: {role}");

            Console.WriteLine("[Refresh] Issuing new access token");
            var access = TokenService.Instance.IssueAccessToken(rec.UserId, role);

            Console.WriteLine("[Refresh] Creating new refresh token");
            var (newRaw, newHash, newExpUtc) = TokenService.Instance.CreateRefreshToken();

            var (ua, ip) = GetUaAndIp(req);
            Console.WriteLine("[Refresh] Inserting new refresh token into database");
            await repo.InsertAsync(rec.UserId, newHash, newExpUtc, ua, ip);

            var handler = new JwtSecurityTokenHandler();
            var parsed = handler.ReadJwtToken(access);
            var accessExpUtc = parsed.ValidTo.ToUniversalTime();
            var accessExpiresIn = (int)Math.Max(0, (accessExpUtc - DateTime.UtcNow).TotalSeconds);
            var refreshExpiresIn = (int)Math.Max(0, (newExpUtc - DateTime.UtcNow).TotalSeconds);

            var resp = new RefreshResponse.TokenBundleResponse
            {
                AccessToken = access,
                ExpiresIn = accessExpiresIn,
                AccessExpiresAt = accessExpUtc,

                RefreshToken = newRaw,
                RefreshExpiresIn = refreshExpiresIn,
                RefreshExpiresAt = newExpUtc,

                User = new RefreshResponse.UserPayload { Id = rec.UserId, Role = role }
            };

            Console.WriteLine("[Refresh] Successfully completed refresh token flow");
            return Success200(resp);
        }

        private static async Task<APIGatewayProxyResponse> Logout(APIGatewayProxyRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Body))
                return Success200();

            var dto = JsonSerializer.Deserialize<LogoutRequest>(req.Body);
            if (string.IsNullOrWhiteSpace(dto?.RefreshToken))
                return Success200();

            var hash = TokenService.HashRefreshRaw(dto.RefreshToken);

            var repo = new TokenRepository();
            await repo.RevokeAsync(hash);

            return Success200();
        }

        private static Task<APIGatewayProxyResponse> Verify(APIGatewayProxyRequest req)
        {
            var auth = req.Headers != null && req.Headers.TryGetValue("Authorization", out var v) ? v : null;
            var (result, user) = JwtService.Instance.ValidateBearer(auth);

            switch (result)
            {
                case JwtValidationResult.Missing:
                    return Task.FromResult(ErrorResults.Json(ErrorCode.AuthTokenMissing));

                case JwtValidationResult.Expired:
                    return Task.FromResult(ErrorResults.Json(ErrorCode.AuthTokenExpired));

                case JwtValidationResult.Invalid:
                    return Task.FromResult(ErrorResults.Json(ErrorCode.AuthTokenInvalid));

                case JwtValidationResult.Valid:
                    var sub = user!.FindFirst("sub")?.Value ?? user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                    var role = user.FindFirst("role")?.Value;
                    var exp = user.FindFirst("exp")?.Value;

                    var body = new VerifyResponse { Sub = sub, Role = role, Exp = exp, Valid = true };
                    return Task.FromResult(Success200(body));

                default:
                    return Task.FromResult(ErrorResults.Json(ErrorCode.Unauthorized));
            }
        }

        #endregion

        #region !============================ Helpers ============================!

        private static (string ua, string ip) GetUaAndIp(APIGatewayProxyRequest req)
        {
            string ua = "";
            if (req.Headers != null && req.Headers.TryGetValue("User-Agent", out var v)) ua = v;
            var ip = req.RequestContext?.Identity?.SourceIp ?? "";
            return (ua, ip);
        }

        private static async Task<string> ResolveRoleAsync(string userId)
        {
            try
            {
                var arn = Environment.GetEnvironmentVariable("AUTH_ACCOUNTS_SECRET_ARN");
                if (string.IsNullOrWhiteSpace(arn)) return "user";

                var raw = await SecretsService.Instance.GetSecretRawAsync(arn);

                using var doc = JsonDocument.Parse(raw);
                if (doc.RootElement.TryGetProperty("accounts", out var arr) && arr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var e in arr.EnumerateArray())
                    {
                        var id = e.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
                        if (string.Equals(id, userId, StringComparison.Ordinal))
                        {
                            var role = e.TryGetProperty("role", out var rEl) ? rEl.GetString() : null;
                            return string.IsNullOrWhiteSpace(role) ? "user" : role!;
                        }
                    }
                }
            }
            catch
            {
                /* fallback */
            }

            return "user";
        }
        
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