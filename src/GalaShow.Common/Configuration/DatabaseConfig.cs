using System;

namespace GalaShow.Common.Configuration
{
    public class DatabaseConfig
    {
        public StageConfig.Stage Environment { get; }
        public string SecretArn { get; }
        public string Server { get; }
        public uint Port { get; }
        public string Database { get; }
        
        public DatabaseConfig()
        {
            string environment = System.Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "dev";
            Environment = environment.ToLower()[0] == 'p' ? StageConfig.Stage.Prod : StageConfig.Stage.Dev;
            
            switch (Environment)
            {
                case StageConfig.Stage.Prod:
                    SecretArn = "rds!db-7e99ba07-7a4a-4108-96b3-107b6a6832a8";
                    Server = "galashow-prod-db.czywcyua8hiu.ap-northeast-2.rds.amazonaws.com";
                    Database = "galashow_prod";
                    break;
                case StageConfig.Stage.Dev:
                    SecretArn = "rds!db-61c5a75e-03e7-49b2-a93f-62c9a0dbb814";
                    Server = "galashow-dev-db.czywcyua8hiu.ap-northeast-2.rds.amazonaws.com";
                    Database = "galashow_dev";
                    break;
            }
            
            Port = 7459;
        }
    }
}