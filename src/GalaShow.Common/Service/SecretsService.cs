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
        private static readonly Dictionary<string, List<string>> CorsCache = new();

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

        public async Task<List<string>> GetCorsAllowedOriginsAsync(string secretId, string stage)
        {
            var cacheKey = $"{secretId}_{stage}";
            if (CorsCache.TryGetValue(cacheKey, out var cached)) return cached;

            var raw = await GetSecretRawAsync(secretId);
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.TryGetProperty(stage, out var originsElement) && originsElement.ValueKind == JsonValueKind.Array)
            {
                var origins = originsElement.EnumerateArray().Select(e => e.GetString()!).ToList();
                CorsCache[cacheKey] = origins;
                return origins;
            }

            return new List<string>();
        }

        public override void Dispose()
        {
            _client?.Dispose();
            base.Dispose();
        }
    }
}