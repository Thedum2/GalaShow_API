using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace GalaShow.Common.Auth
{
    public sealed class JwtValidator
    {
        private readonly JwtSecurityTokenHandler _handler = new();
        private readonly TokenValidationParameters _tvp;

        public JwtValidator(JwtOptions opts, SymmetricSecurityKey key)
        {
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
        }

        public (JwtValidationResult result, ClaimsPrincipal? principal) ValidateBearerDetailed(string? authorization)
        {
            if (string.IsNullOrWhiteSpace(authorization))
            {
                return (JwtValidationResult.Missing, null);
            }

            if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return (JwtValidationResult.Invalid, null);
            }

            var token = authorization.Substring("Bearer ".Length).Trim();

            try
            {
                var principal = _handler.ValidateToken(token, _tvp, out _);
                return (JwtValidationResult.Valid, principal);
            }
            catch (SecurityTokenExpiredException)
            {
                return (JwtValidationResult.Expired, null);
            }
            catch (SecurityTokenException)
            {
                return (JwtValidationResult.Invalid, null);
            }
            catch (Exception)
            {
                return (JwtValidationResult.Invalid, null);
            }
        }

        public ClaimsPrincipal? ValidateBearer(string? authorization)
        {
            var (result, principal) = ValidateBearerDetailed(authorization);
            return result == JwtValidationResult.Valid ? principal : null;
        }
    }
}