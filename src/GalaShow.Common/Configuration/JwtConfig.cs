namespace GalaShow.Common.Configuration
{
    public class JwtConfig
    {
        public string Issuer { get; }
        public string Audience { get; }
        public string SecretArn { get; }

        public JwtConfig()
        {
            Issuer    = Environment.GetEnvironmentVariable("JWT_ISSUER")   ?? "galashow";
            Audience  = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "galashow-client";
            SecretArn = Environment.GetEnvironmentVariable("JWT_SECRET_ARN") ?? "";
        }
    }
}