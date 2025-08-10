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
                SecretArn = "rds!db-61c5a75e-03e7-49b2-a93f-62c9a0dbb814";
                Server = "galashow-dev-db.czywcyua8hiu.ap-northeast-2.rds.amazonaws.com";
                Database = "galashow_dev";
            }
            else
            {
                SecretArn = "rds!db-7e99ba07-7a4a-4108-96b3-107b6a6832a8";
                Server = "galashow-prod-db.czywcyua8hiu.ap-northeast-2.rds.amazonaws.com";
                Database = "galashow_prod";
            }
            Port = 7459;
        }
    }
}