using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using GalaShow.Common.Infrastructure;
using GalaShow.Common.Models;

namespace GalaShow.Common.Service
{
    public sealed class SecretsService : AsyncSingleton<SecretsService>, IDisposable
    {
        private IAmazonSecretsManager? _client;
        private static readonly Dictionary<string, DbCredentials> DbCache = new();

        private SecretsService() { }

        protected override Task InitializeCoreAsync()
        {
            _client ??= new AmazonSecretsManagerClient();
            return Task.CompletedTask;
        }

        public async Task<string> GetSecretRawAsync(string secretId)
        {
            if (_client is null) throw new InvalidOperationException("SecretsService not initialized.");
            var resp = await _client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretId });
            return resp.SecretString;
        }

        public async Task<DbCredentials> GetDbCredentialsAsync(string secretId)
        {
            if (DbCache.TryGetValue(secretId, out var cached)) return cached;

            var raw = await GetSecretRawAsync(secretId);
            var creds = JsonSerializer.Deserialize<DbCredentials>(raw)
                        ?? throw new InvalidOperationException("Failed to deserialize DB credentials");
            DbCache[secretId] = creds;
            return creds;
        }

        public override void Dispose()
        {
            _client?.Dispose();
            base.Dispose();
        }
    }
}