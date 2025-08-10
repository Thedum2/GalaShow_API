using System.Security.Claims;
using System.Text;
using System.Text.Json;
using GalaShow.Common.Auth;
using GalaShow.Common.Infrastructure;
using Microsoft.IdentityModel.Tokens;

namespace GalaShow.Common.Service
{
    public class JwtService : AsyncSingleton<JwtService>
    {
        private JwtOptions? _opts;
        private SymmetricSecurityKey? _key;
        private JwtCreator? _creator;
        private JwtValidator? _validator;

        private JwtService() { }

        protected override async Task InitializeCoreAsync()
        {
            _opts = JwtOptions.FromEnv();
            if (string.IsNullOrWhiteSpace(_opts.SecretArn))
                throw new InvalidOperationException("JWT secret ARN is empty.");

            var raw = await SecretsService.Instance.GetSecretRawAsync(_opts.SecretArn);

            string hs256Key;
            try
            {
                using var doc = JsonDocument.Parse(raw);
                hs256Key = doc.RootElement.GetProperty("Hs256Key").GetString() ?? "";
            }
            catch
            {
                throw new InvalidOperationException("JWT secret JSON must contain Hs256Key.");
            }
            if (string.IsNullOrWhiteSpace(hs256Key))
                throw new InvalidOperationException("Hs256Key is empty.");

            _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(hs256Key));
            _creator   = new JwtCreator(_opts, _key);
            _validator = new JwtValidator(_opts, _key);
        }

        private JwtCreator Creator => _creator ?? throw new InvalidOperationException("JwtService not initialized.");
        private JwtValidator Validator => _validator ?? throw new InvalidOperationException("JwtService not initialized.");


        public string IssueAccessToken(string sub, string role, int minutes) => Creator.IssueAccessToken(sub, role, minutes);

        public string IssueCustomToken(IEnumerable<Claim> claims, TimeSpan lifetime, string? issuerOverride = null, string? audienceOverride = null) => Creator.IssueToken(claims, lifetime, issuerOverride, audienceOverride);

        public ClaimsPrincipal ValidateBearer(string? authorization) => Validator.ValidateBearer(authorization);

        public override void Dispose() => base.Dispose();
    }
}
