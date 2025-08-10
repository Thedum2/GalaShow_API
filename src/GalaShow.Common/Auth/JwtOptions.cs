namespace GalaShow.Common.Auth
{
    public class JwtOptions
    {
        public string? Issuer { get; private init; } = "galashow";
        public string? Audience { get; private init; } = "galashow-client";
        public string? SecretArn { get; private init; } = string.Empty;

        private const string DevSecretArn = "arn:aws:secretsmanager:ap-northeast-2:610495549763:secret:dev/galashow-kyCunF";
        private const string ProdSecretArn = "arn:aws:secretsmanager:ap-northeast-2:610495549763:secret:prod/galashow-AuW7Z5";
        public static JwtOptions FromEnv()
        {
            var stageName =
                Environment.GetEnvironmentVariable("STAGE") ??
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ??
                Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

            var isDev = IsDevelopment(stageName);

            var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "galashow";
            var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "galashow-client";

            var secretArn = isDev ? DevSecretArn : ProdSecretArn;

            return new JwtOptions
            {
                Issuer = issuer,
                Audience = audience,
                SecretArn = secretArn
            };
        }

        public static bool IsDevelopment(string? stage)
        {
            if (string.IsNullOrWhiteSpace(stage)) return false;
            return stage.Equals("dev", StringComparison.OrdinalIgnoreCase) || stage.Equals("development", StringComparison.OrdinalIgnoreCase);
        }
    }
}