using GalaShow.ChzzkProxy;

namespace GalaShow.Token.Tests;

[Collection("Environment configuration")]
public sealed class ChzzkCredentialsTests : ConfigurationEnvironment
{
    [Fact]
    public async Task ServerEnvironment_IsUsedWithoutAws()
    {
        Set("CHZZK_CLIENT_ID", "local-id");
        Set("CHZZK_CLIENT_SECRET", "local-secret");
        var provider = new ChzzkCredentialsProvider(_ => throw new InvalidOperationException("AWS must not be called"));
        Assert.Equal(new ChzzkCredentials("local-id", "local-secret"), await provider.GetAsync());
    }

    [Fact]
    public async Task SecretArn_TakesPrecedence_AndConcurrentReadsAreCached()
    {
        Set("CHZZK_SECRET_ARN", "test-arn");
        Set("CHZZK_CLIENT_ID", "ignored-id");
        Set("CHZZK_CLIENT_SECRET", "ignored-secret");
        var reads = 0;
        var provider = new ChzzkCredentialsProvider(async arn =>
        {
            Assert.Equal("test-arn", arn);
            Interlocked.Increment(ref reads);
            await Task.Yield();
            return "{\"clientId\":\"configured-id\",\"clientSecret\":\"configured-secret\"}";
        });
        var results = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => provider.GetAsync()));
        Assert.All(results, value => Assert.Equal(new ChzzkCredentials("configured-id", "configured-secret"), value));
        Assert.Equal(1, reads);
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("{}")]
    [InlineData("{\"clientId\":\"id\",\"clientSecret\":\"\"}")]
    [InlineData("{\"clientId\":\"id\",\"clientSecret\":\"sensitive\\r\\nvalue\"}")]
    public async Task InvalidSecret_IsSanitizedWithoutEnvironmentFallback(string secret)
    {
        Set("CHZZK_SECRET_ARN", "test-arn");
        Set("CHZZK_CLIENT_ID", "fallback-id");
        Set("CHZZK_CLIENT_SECRET", "fallback-secret");
        var provider = new ChzzkCredentialsProvider(_ => Task.FromResult(secret));
        var exception = await Assert.ThrowsAsync<ChzzkConfigurationException>(provider.GetAsync);
        Assert.DoesNotContain(secret, exception.ToString());
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public async Task SecretFailure_IsSanitizedAndRetried()
    {
        Set("CHZZK_SECRET_ARN", "test-arn");
        var reads = 0;
        var provider = new ChzzkCredentialsProvider(_ => ++reads == 1
            ? throw new InvalidOperationException("sensitive configuration details")
            : Task.FromResult("{\"clientId\":\"id\",\"clientSecret\":\"secret\"}"));
        var exception = await Assert.ThrowsAsync<ChzzkConfigurationException>(provider.GetAsync);
        Assert.DoesNotContain("sensitive", exception.ToString());
        Assert.Equal("id", (await provider.GetAsync()).ClientId);
        Assert.Equal(2, reads);
    }
}
