using System;

namespace GalaShow.Common.Configuration
{
    public class DatabaseConfig
    { public string SecretArn { get; }
        public string Server { get; }
        public uint Port { get; }
        public string Database { get; }
        
        public DatabaseConfig()
        {
            if (StageResolver.IsDev())
            {
                SecretArn = "rds!db-3230a24e-c513-4343-b32d-4b082afea0e5";
                Server = "galashow-db-dev.czywcyua8hiu.ap-northeast-2.rds.amazonaws.com";
                Database = "galashow";
            }
            else
            {
                SecretArn = "test";
                Server = "test";
                Database = "test";
            }
            Port = 7459;
        }
    }
}