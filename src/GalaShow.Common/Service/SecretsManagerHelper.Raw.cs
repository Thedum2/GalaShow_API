using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace GalaShow.Common.Service
{
    public static class SecretsManagerHelperRawExtensions
    {
        public static async Task<string> GetSecretRawAsync(this SecretsManagerHelper _, string secretId)
        {
            using var client = new AmazonSecretsManagerClient();
            var resp = await client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretId });
            return resp.SecretString;
        }
    }
}