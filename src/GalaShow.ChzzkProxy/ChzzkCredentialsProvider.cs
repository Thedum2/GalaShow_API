using System.Text.Json;
using GalaShow.Common.Service;

namespace GalaShow.ChzzkProxy;

public sealed record ChzzkCredentials(string ClientId, string ClientSecret);

public sealed class ChzzkConfigurationException : Exception
{
    public ChzzkConfigurationException() : base("CHZZK configuration is unavailable.") { }
}

/// <summary>Loads only server configuration; never accepts credentials from an HTTP request.</summary>
public sealed class ChzzkCredentialsProvider
{
    private readonly Func<string, Task<string>> _readSecret;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private ChzzkCredentials? _cached;
    private string? _cachedArn;
    private DateTimeOffset _expiresAt;

    public ChzzkCredentialsProvider(Func<string, Task<string>>? readSecret = null)
        => _readSecret = readSecret ?? ReadSecretAsync;

    public async Task<ChzzkCredentials> GetAsync()
    {
        var arn = Environment.GetEnvironmentVariable("CHZZK_SECRET_ARN");
        if (string.IsNullOrWhiteSpace(arn))
            return Validate(Environment.GetEnvironmentVariable("CHZZK_CLIENT_ID"),
                Environment.GetEnvironmentVariable("CHZZK_CLIENT_SECRET"));

        await _gate.WaitAsync();
        try
        {
            if (_cached is not null && _cachedArn == arn && DateTimeOffset.UtcNow < _expiresAt)
                return _cached;
            using var document = JsonDocument.Parse(await _readSecret(arn));
            var json = document.RootElement;
            var credentials = Validate(json.GetProperty("clientId").GetString(), json.GetProperty("clientSecret").GetString());
            _cached = credentials;
            _cachedArn = arn;
            _expiresAt = DateTimeOffset.UtcNow.AddMinutes(5);
            return credentials;
        }
        catch (Exception)
        {
            // Neither the provider exception nor a secret's contents may reach a response/log.
            throw new ChzzkConfigurationException();
        }
        finally { _gate.Release(); }
    }

    private static ChzzkCredentials Validate(string? clientId, string? clientSecret)
    {
        if (!Valid(clientId) || !Valid(clientSecret)) throw new ChzzkConfigurationException();
        return new(clientId!, clientSecret!);
        static bool Valid(string? value) => !string.IsNullOrWhiteSpace(value) && value.All(c => c > 32 && c < 127);
    }

    private static async Task<string> ReadSecretAsync(string arn)
    {
        // CHZZK needs public internet access, not the application's private database bootstrap.
        await SecretsService.Instance.InitializeAsync();
        return await SecretsService.Instance.GetSecretRawAsync(arn);
    }
}
