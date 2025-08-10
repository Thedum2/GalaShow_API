using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GalaShow.Common.Service;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;

namespace GalaShow.Common.Auth
{
    public interface IJwtValidator
    {
        Task InitializeAsync();
        ClaimsPrincipal ValidateBearer(string? authorization);
    }

    public sealed class JwtValidator(JwtOptions opts) : IJwtValidator, IDisposable
    {
        private readonly JwtSecurityTokenHandler _handler = new();
        private TokenValidationParameters? _tvp;
        private bool _initialized;
        private readonly SecretsManagerHelper _secrets = new();

        public async Task InitializeAsync()
        {
            if (_initialized)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(opts.SecretArn))
            {
                throw new InvalidOperationException("JWT_SECRET_ARN is not set.");
            }

            var raw = await _secrets.GetSecretRawAsync(opts.SecretArn);
            string hs256Key;
            try
            {
                using var doc = JsonDocument.Parse(raw);
                hs256Key = doc.RootElement.GetProperty("Hs256Key").GetString() ?? "";
            }
            catch
            {
                throw new InvalidOperationException("JWT secret JSON must contain Hs256Key property.");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(hs256Key));
            _tvp = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = opts.Issuer,
                ValidateAudience = true,
                ValidAudience = opts.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                RequireExpirationTime = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(2)
            };

            _initialized = true;
        }

        public ClaimsPrincipal ValidateBearer(string? authorization)
        {
            if (!_initialized || _tvp is null)
            {
                throw new InvalidOperationException("JwtValidator is not initialized.");
            }

            if (string.IsNullOrWhiteSpace(authorization))
            {
                throw new SecurityTokenException("Missing Authorization header");
            }

            if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                throw new SecurityTokenException("Invalid auth scheme");
            }

            var token = authorization.Substring("Bearer ".Length).Trim();
            return _handler.ValidateToken(token, _tvp, out _);
        }

        public void Dispose() => _secrets.Dispose();
    }
}
