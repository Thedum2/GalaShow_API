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

        public ClaimsPrincipal ValidateBearer(string? authorization)
        {
            if (string.IsNullOrWhiteSpace(authorization))
                throw new SecurityTokenException("Missing Authorization header");

            if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                throw new SecurityTokenException("Invalid auth scheme");

            var token = authorization["Bearer ".Length..].Trim();
            return _handler.ValidateToken(token, _tvp, out _);
        }
    }
}