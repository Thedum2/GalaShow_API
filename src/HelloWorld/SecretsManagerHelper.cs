using System;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace HelloWorld
{
    public class SecretsManagerHelper
    {
        private readonly IAmazonSecretsManager _client = new AmazonSecretsManagerClient();

        public async Task<DbCredentials> GetDbCredentialsAsync(string secretName)
        {
            var request = new GetSecretValueRequest
            {
                SecretId = secretName
            };
            var response = await _client.GetSecretValueAsync(request);
            var credentials = JsonSerializer.Deserialize<DbCredentials>(response.SecretString);
            if (credentials == null)
                throw new Exception("SecretsManager returned null");
            return credentials;
        }
    }

    public class DbCredentials
    {
        public string username { get; set; }
        public string password { get; set; }
    }
}