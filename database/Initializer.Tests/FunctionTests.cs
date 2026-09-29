using Xunit;

namespace GalaShow.DatabaseInitializer.Tests;

public sealed class FunctionTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Initialize")]
    [InlineData("initialize ")]
    [InlineData("drop")]
    [InlineData("migrate-banner-slots ")]
    [InlineData("Migrate-banner-slots")]
    public async Task UnsupportedActionIsRejectedBeforeReadingConfigurationOrContactingAws(string? action)
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            new Function().FunctionHandler(new InitializationRequest { Action = action }, null!));

        Assert.Contains("action must be exactly 'initialize'", exception.Message);
    }

    [Fact]
    public async Task MissingRequestIsRejectedBeforeReadingConfigurationOrContactingAws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => new Function().FunctionHandler(null, null!));
    }

    [Fact]
    public async Task BannerMigrationRequiresDatabaseConfigurationBeforeContactingAws()
    {
        var original = Environment.GetEnvironmentVariable("DB_HOST");
        try
        {
            Environment.SetEnvironmentVariable("DB_HOST", null);
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                new Function().FunctionHandler(new InitializationRequest { Action = "migrate-banner-slots" }, null!));
            Assert.Contains("DB_HOST", exception.Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable("DB_HOST", original);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("prod")]
    [InlineData("DEV")]
    public async Task SampleSeedIsRejectedOutsideExplicitDevelopmentStage(string? stage)
    {
        var original = Environment.GetEnvironmentVariable("STAGE");
        try
        {
            Environment.SetEnvironmentVariable("STAGE", stage);
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                new Function().FunctionHandler(new InitializationRequest { Action = "seed-dev-samples" }, null!));
            Assert.Contains("development", exception.Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable("STAGE", original);
        }
    }

    [Fact]
    public async Task DevelopmentSampleSeedRequiresDatabaseConfiguration()
    {
        var originalStage = Environment.GetEnvironmentVariable("STAGE");
        var originalHost = Environment.GetEnvironmentVariable("DB_HOST");
        try
        {
            Environment.SetEnvironmentVariable("STAGE", "dev");
            Environment.SetEnvironmentVariable("DB_HOST", null);
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                new Function().FunctionHandler(new InitializationRequest { Action = "seed-dev-samples" }, null!));
            Assert.Contains("DB_HOST", exception.Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable("STAGE", originalStage);
            Environment.SetEnvironmentVariable("DB_HOST", originalHost);
        }
    }
}
