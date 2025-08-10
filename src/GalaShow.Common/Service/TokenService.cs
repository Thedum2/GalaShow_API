using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Amazon.Lambda.APIGatewayEvents;
using GalaShow.Common.Configuration;
using GalaShow.Common.Infrastructure;
using Microsoft.IdentityModel.Tokens;

namespace GalaShow.Common.Service
{
    public sealed class TokenService : AsyncSingleton<TokenService>, IDisposable
    {
        private int _accessMinutes;
        private int _refreshDays;

        private TokenService() { }

        protected override Task InitializeCoreAsync()
        {
            _accessMinutes = GetIntEnv("ACCESS_TOKEN_MINUTES", (int)TokenDefaults.AccessMinutes);
            _refreshDays   = GetIntEnv("REFRESH_TOKEN_DAYS",   (int)TokenDefaults.RefreshDays);
            return Task.CompletedTask;
        }

        public string IssueAccessToken(string sub, string role)
            => JwtService.Instance.IssueAccessToken(sub, role, _accessMinutes);

        public (string raw, string hash, DateTime expiresAt) CreateRefreshToken()
        {
            Span<byte> buf = stackalloc byte[32];
            RandomNumberGenerator.Fill(buf);

            var raw  = Convert.ToBase64String(buf).TrimEnd('=').Replace('+','-').Replace('/','_');
            var hash = Sha256Hex(raw);
            var exp  = DateTime.UtcNow.AddDays(_refreshDays);
            return (raw, hash, exp);
        }

        public static string HashRefreshRaw(string raw) => Sha256Hex(raw);

        public Task<(bool ok, string role)> ValidateCredentialAsync(string id, string password)
        {
            //TODO:: 추후 인증 로직 구현
            return Task.FromResult((true, "user"));
        }

        private static int GetIntEnv(string key, int def)
            => int.TryParse(Environment.GetEnvironmentVariable(key), out var v) ? v : def;

        private static string Sha256Hex(string s)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(s));
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }

        public async Task<APIGatewayProxyResponse?> RequireAuthThen(
            APIGatewayProxyRequest req,
            Func<ClaimsPrincipal, Task<APIGatewayProxyResponse>> next,
            Func<APIGatewayProxyResponse> onExpired,
            Func<APIGatewayProxyResponse> onUnauthorized)
        {
            if (req.Headers is null ||
                !req.Headers.TryGetValue("Authorization", out var auth) ||
                string.IsNullOrWhiteSpace(auth))
                return onUnauthorized();

            try
            {
                var user = JwtService.Instance.ValidateBearer(auth);
                return await next(user);
            }
            catch (SecurityTokenExpiredException)
            {
                return onExpired();
            }
            catch (SecurityTokenException)
            {
                return onUnauthorized();
            }
        }
        
        public override void Dispose() => base.Dispose();

        private sealed class AccountsRoot
        {
            public List<Account> Accounts { get; set; } = new();
        }

        private sealed class Account
        {
            public string Id { get; set; } = "";
            public string Role { get; set; } = "user";
            public string Salt { get; set; } = "";
            public string PasswordHash { get; set; } = "";
        }
    }
}
