using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using GalaShow.Common;
using MySql.Data.MySqlClient;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace HelloWorld
{
    public class Function
    {
        public Task<APIGatewayProxyResponse> FunctionHandler(
            APIGatewayProxyRequest input,
            ILambdaContext context)
        {
            // (중복 호출 안전)
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            /*
             *
            // Secrets Manager에서 자격증명 조회
            var db = await new SecretsManagerHelper()
                .GetDbCredentialsAsync("rds!db-61c5a75e-03e7-49b2-a93f-62c9a0dbb814");

            context.Logger.LogInformation($"User: {db.username}");
            context.Logger.LogInformation($"Password: {db.password}");

            // MySQL 연결 문자열 설정
            var builder = new MySqlConnectionStringBuilder
            {
                Server       = "galashow-dev-db.czywcyua8hiu.ap-northeast-2.rds.amazonaws.com",
                Port         = 7459,
                Database     = "galashow_dev",
                UserID       = db.username,
                Password     = db.password,
                CharacterSet = "utf8mb4",
                SslMode      = MySqlSslMode.None
            };

            string result;
            try
            {
                await using var conn = new MySqlConnection(builder.ConnectionString);
                await conn.OpenAsync();

                await using var cmd = new MySqlCommand(
                  "SELECT message FROM greetings LIMIT 1", conn);
                var message = (string?)await cmd.ExecuteScalarAsync();
                result = message ?? "No message found";
            }
            catch (Exception ex)
            {
                context.Logger.LogError(ex, "DB Error");
                result = $"DB Error: {ex.Message}";
            }

            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Body = result,
                Headers = new Dictionary<string, string>
                {
                    ["Content-Type"] = "text/plain; charset=utf-8"
                }
            };
             */
            return Task.FromResult(new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Body = new Class1().TT(),
                Headers = new Dictionary<string, string>
                {
                    ["Content-Type"] = "text/plain; charset=utf-8"
                }
            });
        }
    }
}