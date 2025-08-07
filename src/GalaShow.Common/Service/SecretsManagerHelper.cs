using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using GalaShow.Common.Models;

namespace GalaShow.Common.Service
{
    public class SecretsManagerHelper : IDisposable
    {
        private readonly IAmazonSecretsManager _client;

        public SecretsManagerHelper()
        {
            _client = new AmazonSecretsManagerClient();
        }

        public async Task<DbCredentials> GetDbCredentialsAsync(string secretName)
        {
            try
            {
                var request = new GetSecretValueRequest { SecretId = secretName };
                var response = await _client.GetSecretValueAsync(request);
                
                var credentials = JsonSerializer.Deserialize<DbCredentials>(response.SecretString);
                return credentials ?? throw new InvalidOperationException("Failed to deserialize credentials");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve credentials: {ex.Message}", ex);
            }
        }

        public void Dispose() => _client?.Dispose();
    }
}