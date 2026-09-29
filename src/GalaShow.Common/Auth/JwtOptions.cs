namespace GalaShow.Common.Auth
{
    public class JwtOptions
    {
        public string? Issuer { get; init; } = "galashow";
        public string? Audience { get; init; } = "galashow-client";
        public string? SecretArn { get; init; } = string.Empty;

        public static JwtOptions FromEnv()
        {
            var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "galashow";
            var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "galashow-client";

            var secretArn = Environment.GetEnvironmentVariable("JWT_SECRET_ARN");
            if (string.IsNullOrWhiteSpace(secretArn))
                throw new InvalidOperationException("JWT_SECRET_ARN must be configured.");

            return new JwtOptions
            {
                Issuer = issuer,
                Audience = audience,
                SecretArn = secretArn.Trim()
            };
        }
    }
}
