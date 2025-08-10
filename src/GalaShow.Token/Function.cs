using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
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
            await AppBootstrap.InitAsync();

            try
            {
                return (req.HttpMethod, req.Path) switch
                {
                    ("POST", "/auth/login") => await Login(req),
                    ("POST", "/auth/refresh") => await Refresh(req),
                    ("POST", "/auth/logout") => await Logout(req),
                    ("GET", "/auth/verify") => await Verify(req),
                    _ => NotFound()
                };
            }
            catch (SecurityTokenException ste)
            {
                ctx.Logger.LogError($"Auth error: {ste.Message}");
                return Unauthorized(ste.Message);
            }
            catch (Exception ex)
            {
                ctx.Logger.LogError(ex.ToString());
                return ServerError("Internal server error");
            }
        }

        #region !============================Handlers============================!

        private static async Task<APIGatewayProxyResponse> Login(APIGatewayProxyRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Body)) return Unauthorized("Empty body");

            var dto = JsonSerializer.Deserialize<LoginRequest>(req.Body);
            if (dto is null || string.IsNullOrWhiteSpace(dto.Id) || string.IsNullOrWhiteSpace(dto.Password))
                return Unauthorized("Invalid credentials");

            var (ok, role) = await TokenService.Instance.ValidateCredentialAsync(dto.Id, dto.Password);
            if (!ok) return Unauthorized("Invalid credentials");

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
            return Json200(resp);
        }

        private static async Task<APIGatewayProxyResponse> Refresh(APIGatewayProxyRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Body)) return Unauthorized("No body");
            var dto = JsonSerializer.Deserialize<RefreshRequest>(req.Body);
            if (string.IsNullOrWhiteSpace(dto?.RefreshToken)) return Unauthorized("No refreshToken");

            var hash = TokenService.HashRefreshRaw(dto.RefreshToken);

            var repo = new TokenRepository();
            var rec = await repo.GetByHashAsync(hash);
            if (rec is null) return Unauthorized("Invalid refreshToken");
            if (rec.RevokedAt.HasValue) return Unauthorized("Refresh revoked");
            if (rec.ExpiresAt <= DateTime.UtcNow) return Unauthorized("Refresh expired");

            await repo.RevokeAsync(hash);

            var role = await ResolveRoleAsync(rec.UserId);

            var access = TokenService.Instance.IssueAccessToken(rec.UserId, role);
            var (newRaw, newHash, newExpUtc) = TokenService.Instance.CreateRefreshToken();

            var (ua, ip) = GetUaAndIp(req);
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
            return Json200(resp);
        }

        private static async Task<APIGatewayProxyResponse> Logout(APIGatewayProxyRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Body))
                return Json200(new { ok = true });

            var dto = JsonSerializer.Deserialize<LogoutRequest>(req.Body);
            if (string.IsNullOrWhiteSpace(dto?.RefreshToken))
                return Json200(new { ok = true });

            var hash = TokenService.HashRefreshRaw(dto.RefreshToken);

            var repo = new TokenRepository();
            await repo.RevokeAsync(hash);

            return Json200(new { ok = true });
        }

        private static Task<APIGatewayProxyResponse> Verify(APIGatewayProxyRequest req)
        {
            var auth = req.Headers != null && req.Headers.TryGetValue("Authorization", out var v) ? v : null;
            var user = JwtService.Instance.ValidateBearer(auth);

            var sub = user.FindFirst("sub")?.Value ?? user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            var role = user.FindFirst("role")?.Value;
            var exp = user.FindFirst("exp")?.Value;

            var body = new VerifyResponse { Sub = sub, Role = role, Exp = exp, Valid = true };
            return Task.FromResult(Json200(body));
        }

        #endregion

        #region !============================Helpers============================!

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

        private static Dictionary<string, string> JsonHeaders(bool allowCredentials = false) => new()
        {
            ["Content-Type"] = "application/json; charset=utf-8",
            ["Access-Control-Allow-Origin"] = "*",
            ["Access-Control-Allow-Credentials"] = allowCredentials ? "true" : "false"
        };

        #endregion

        #region !============================Responses============================!

        private static APIGatewayProxyResponse Json200<T>(T body, bool allowCredentials = false) => new()
        {
            StatusCode = 200,
            Headers = JsonHeaders(allowCredentials),
            Body = JsonSerializer.Serialize(ApiResponse<T>.SuccessResult(body))
        };

        private static APIGatewayProxyResponse Unauthorized(string msg = "Unauthorized") => new()
        {
            StatusCode = 401,
            Headers = JsonHeaders(),
            Body = JsonSerializer.Serialize(ApiResponse<object>.ErrorResult(msg, null, "401"))
        };

        private static APIGatewayProxyResponse NotFound() => new()
        {
            StatusCode = 404,
            Headers = JsonHeaders(),
            Body = JsonSerializer.Serialize(ApiResponse<object>.ErrorResult("Not Found", null, "404"))
        };

        private static APIGatewayProxyResponse ServerError(string msg) => new()
        {
            StatusCode = 500,
            Headers = JsonHeaders(),
            Body = JsonSerializer.Serialize(ApiResponse<object>.ErrorResult(msg, null, "500"))
        };

        #endregion
    }
}