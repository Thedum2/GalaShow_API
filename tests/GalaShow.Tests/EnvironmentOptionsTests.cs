using GalaShow.Common.Auth;
using GalaShow.Common.Configuration;

namespace GalaShow.Token.Tests;

[Collection("Environment configuration")]
public sealed class EnvironmentOptionsTests : ConfigurationEnvironment
{
    [Fact]
    public void Database_UsesDeploymentValuesAndMySqlDefaults()
    {
        Set("DB_HOST", "database.internal");
        Set("DB_SECRET_ARN", "arn:aws:secretsmanager:ap-northeast-2:123456789012:secret:db-test");

        var config = new DatabaseConfig();

        Assert.Equal("database.internal", config.Server);
        Assert.Equal("arn:aws:secretsmanager:ap-northeast-2:123456789012:secret:db-test", config.SecretArn);
        Assert.Equal(3306u, config.Port);
        Assert.Equal("galashow", config.Database);
    }

    [Fact]
    public void Database_UsesExplicitPortAndDatabase()
    {
        Set("DB_HOST", "database.internal");
        Set("DB_SECRET_ARN", "database-secret");
        Set("DB_PORT", "7459");
        Set("DB_NAME", "galashow_dev");

        var config = new DatabaseConfig();

        Assert.Equal(7459u, config.Port);
        Assert.Equal("galashow_dev", config.Database);
    }

    [Theory]
    [InlineData("DB_HOST", null)]
    [InlineData("DB_HOST", " ")]
    [InlineData("DB_SECRET_ARN", null)]
    [InlineData("DB_SECRET_ARN", " ")]
    public void Database_RejectsMissingRequiredSettings(string name, string? value)
    {
        Set("DB_HOST", "database.internal");
        Set("DB_SECRET_ARN", "database-secret");
        Set(name, value);

        var error = Assert.Throws<InvalidOperationException>(() => new DatabaseConfig());

        Assert.Contains(name, error.Message);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("65536")]
    [InlineData("-1")]
    [InlineData("mysql")]
    [InlineData(" ")]
    public void Database_RejectsInvalidPorts(string value)
    {
        Set("DB_HOST", "database.internal");
        Set("DB_SECRET_ARN", "database-secret");
        Set("DB_PORT", value);

        var error = Assert.Throws<InvalidOperationException>(() => new DatabaseConfig());

        Assert.Contains("DB_PORT", error.Message);
    }

    [Theory]
    [InlineData("1", 1u)]
    [InlineData("65535", 65535u)]
    public void Database_AcceptsValidPortBoundaries(string value, uint expected)
    {
        Set("DB_HOST", "database.internal");
        Set("DB_SECRET_ARN", "database-secret");
        Set("DB_PORT", value);

        Assert.Equal(expected, new DatabaseConfig().Port);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    public void Jwt_RequiresAnExplicitSecret(string? value)
    {
        Set("JWT_SECRET_ARN", value);

        var error = Assert.Throws<InvalidOperationException>(() => JwtOptions.FromEnv());

        Assert.Contains("JWT_SECRET_ARN", error.Message);
    }

    [Fact]
    public void Jwt_UsesDeploymentSecretAndExistingTokenDefaults()
    {
        Set("JWT_SECRET_ARN", "deployment-jwt-secret");

        var config = JwtOptions.FromEnv();

        Assert.Equal("deployment-jwt-secret", config.SecretArn);
        Assert.Equal("galashow", config.Issuer);
        Assert.Equal("galashow-client", config.Audience);
    }

    [Fact]
    public void Jwt_PreservesIssuerAndAudienceOverrides()
    {
        Set("JWT_SECRET_ARN", "deployment-jwt-secret");
        Set("JWT_ISSUER", "custom-issuer");
        Set("JWT_AUDIENCE", "custom-audience");

        var config = JwtOptions.FromEnv();

        Assert.Equal("custom-issuer", config.Issuer);
        Assert.Equal("custom-audience", config.Audience);
    }
}
