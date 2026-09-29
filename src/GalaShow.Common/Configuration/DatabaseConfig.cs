using System;

namespace GalaShow.Common.Configuration
{
    public class DatabaseConfig
    {
        public string SecretArn { get; }
        public string Server { get; }
        public uint Port { get; }
        public string Database { get; }
        
        public DatabaseConfig()
        {
            Server = RequiredEnvironmentVariable("DB_HOST");
            SecretArn = RequiredEnvironmentVariable("DB_SECRET_ARN");
            Database = Environment.GetEnvironmentVariable("DB_NAME") ?? "galashow";

            var portValue = Environment.GetEnvironmentVariable("DB_PORT") ?? "3306";
            if (!uint.TryParse(portValue, out var port) || port is < 1 or > 65535)
                throw new InvalidOperationException("DB_PORT must be an integer between 1 and 65535.");
            Port = port;
        }

        private static string RequiredEnvironmentVariable(string name)
        {
            var value = Environment.GetEnvironmentVariable(name);
            return !string.IsNullOrWhiteSpace(value)
                ? value.Trim()
                : throw new InvalidOperationException($"{name} must be configured.");
        }
    }
}
