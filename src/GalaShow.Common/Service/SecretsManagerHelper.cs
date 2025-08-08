using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using GalaShow.Common.Models;

namespace GalaShow.Common.Service
{
    public class SecretsManagerHelper : IDisposable
    {
        private readonly IAmazonSecretsManager _client;
        private static readonly Dictionary<string, DbCredentials> _credentialsCache = new();

        public SecretsManagerHelper()
        {
            _client = new AmazonSecretsManagerClient();
        }

        public async Task<DbCredentials> GetDbCredentialsAsync(string secretName)
        {
            if (_credentialsCache.TryGetValue(secretName, out var cached))
            {
                return cached;
            }

            try
            {
                var request = new GetSecretValueRequest { SecretId = secretName };
                var response = await _client.GetSecretValueAsync(request);

                var credentials = JsonSerializer.Deserialize<DbCredentials>(response.SecretString);
                if (credentials == null)
                {
                    throw new InvalidOperationException("Failed to deserialize credentials");
                }
                _credentialsCache[secretName] = credentials;
                return credentials;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve credentials: {ex.Message}", ex);
            }
        }

        public void Dispose() => _client?.Dispose();
    }
}