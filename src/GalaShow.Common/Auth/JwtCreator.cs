using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace GalaShow.Common.Auth
{
    public sealed class JwtCreator
    {
        private readonly JwtOptions _opts;
        private readonly SymmetricSecurityKey _key;

        public JwtCreator(JwtOptions opts, SymmetricSecurityKey key)
        {
            _opts = opts;
            _key  = key;
        }

        public string IssueAccessToken(string sub, string role, int minutes)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, sub),
                new Claim("role", role),
            };
            return IssueToken(claims, TimeSpan.FromMinutes(minutes));
        }

        public string IssueToken(
            IEnumerable<Claim> claims,
            TimeSpan lifetime,
            string? issuerOverride = null,
            string? audienceOverride = null)
        {
            var now   = DateTime.UtcNow;
            var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);

            var allClaims = new List<Claim>(claims)
            {
                new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
            };

            var token = new JwtSecurityToken(
                issuer:  issuerOverride  ?? _opts.Issuer,
                audience:audienceOverride?? _opts.Audience,
                claims:  allClaims,
                notBefore: now,
                expires:   now.Add(lifetime),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}