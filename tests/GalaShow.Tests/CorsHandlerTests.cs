using Amazon.Lambda.APIGatewayEvents;
using GalaShow.Common.Cors;

namespace GalaShow.Token.Tests;

[Collection("Environment configuration")]
public sealed class CorsHandlerTests : ConfigurationEnvironment
{
    [Theory]
    [InlineData("https://dev.galashow.cloud")]
    [InlineData("https://admin-dev.galashow.cloud")]
    public async Task Cors_UsesCommaSeparatedDeploymentOriginsWithoutAws(string origin)
    {
        Set("STAGE", "dev");
        Set("CORS_ALLOWED_ORIGINS", " https://dev.galashow.cloud, https://admin-dev.galashow.cloud, ");
        StageResolver.Resolve();
        await CorsHandler.InitializeAsync();

        var response = CorsHandler.AddCorsHeaders(Request(origin), new APIGatewayProxyResponse());

        Assert.Equal(origin, response!.Headers["Access-Control-Allow-Origin"]);
        Assert.Equal("true", response.Headers["Access-Control-Allow-Credentials"]);
        Assert.Contains("OPTIONS", response.Headers["Access-Control-Allow-Methods"]);
        Assert.Contains("Authorization", response.Headers["Access-Control-Allow-Headers"]);
        Assert.Equal("Origin", response.Headers["Vary"]);
    }

    [Theory]
    [InlineData("dev", "https://dev.galashow.cloud", true)]
    [InlineData("dev", "https://admin-dev.galashow.cloud", true)]
    [InlineData("dev", "http://localhost:5173", true)]
    [InlineData("dev", "http://localhost:3000", true)]
    [InlineData("dev", "https://galashow.cloud", false)]
    [InlineData("prod", "https://galashow.cloud", true)]
    [InlineData("prod", "https://admin.galashow.cloud", true)]
    [InlineData("prod", "https://dev.galashow.cloud", false)]
    [InlineData("prod", "https://admin-dev.galashow.cloud", false)]
    [InlineData("prod", "http://localhost:5173", false)]
    [InlineData("prod", "http://localhost:3000", false)]
    [InlineData("prod", "https://galashow.cloud.attacker.example", false)]
    [InlineData("prod", "null", false)]
    public async Task Cors_DefaultOriginsAreIsolatedByStage(string stage, string origin, bool allowed)
    {
        Set("STAGE", stage);
        StageResolver.Resolve();
        await CorsHandler.InitializeAsync();

        var response = CorsHandler.AddCorsHeaders(Request(origin), new APIGatewayProxyResponse());

        Assert.Equal(allowed, response!.Headers.ContainsKey("Access-Control-Allow-Origin"));
        Assert.Equal("Origin", response.Headers["Vary"]);
    }

    [Fact]
    public async Task Cors_ExplicitAllowlistReplacesDefaultOrigins()
    {
        Set("STAGE", "prod");
        Set("CORS_ALLOWED_ORIGINS", "https://admin.galashow.cloud");
        StageResolver.Resolve();
        await CorsHandler.InitializeAsync();

        var response = CorsHandler.AddCorsHeaders(Request("https://galashow.cloud"), new APIGatewayProxyResponse());

        Assert.False(response!.Headers.ContainsKey("Access-Control-Allow-Origin"));
    }

    [Theory]
    [InlineData("http://localhost")]
    [InlineData("http://localhost:3000")]
    [InlineData("http://localhost:5173")]
    [InlineData("http://localhost:8080")]
    [InlineData("https://localhost")]
    [InlineData("https://localhost:8443")]
    public async Task Cors_DevelopmentAllowsLocalhostRegardlessOfPort(string origin)
    {
        Set("STAGE", "dev");
        Set("CORS_ALLOWED_ORIGINS", "https://dev.galashow.cloud");
        StageResolver.Resolve();
        await CorsHandler.InitializeAsync();

        var response = CorsHandler.AddCorsHeaders(Request(origin), new APIGatewayProxyResponse());

        Assert.Equal(origin, response!.Headers["Access-Control-Allow-Origin"]);
        Assert.Equal("true", response.Headers["Access-Control-Allow-Credentials"]);
    }

    [Theory]
    [InlineData("http://localhost.attacker.example:5173")]
    [InlineData("http://localhost@attacker.example")]
    [InlineData("ftp://localhost:21")]
    public async Task Cors_DevelopmentRejectsOriginsThatOnlyResembleLocalhost(string origin)
    {
        Set("STAGE", "dev");
        StageResolver.Resolve();
        await CorsHandler.InitializeAsync();

        var response = CorsHandler.AddCorsHeaders(Request(origin), new APIGatewayProxyResponse());

        Assert.False(response!.Headers.ContainsKey("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Cors_ProductionNeverAllowsLocalDevelopmentOrigins()
    {
        Set("STAGE", "prod");
        Set("CORS_ALLOWED_ORIGINS", "https://galashow.cloud,http://localhost:5173,http://localhost:3000");
        StageResolver.Resolve();
        await CorsHandler.InitializeAsync();

        var response = CorsHandler.AddCorsHeaders(Request("http://localhost:5173"), new APIGatewayProxyResponse());

        Assert.False(response!.Headers.ContainsKey("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Cors_MissingRequestHeadersAreSafe()
    {
        await CorsHandler.InitializeAsync();

        var response = CorsHandler.AddCorsHeaders(new APIGatewayProxyRequest(), new APIGatewayProxyResponse());

        Assert.False(response!.Headers.ContainsKey("Access-Control-Allow-Origin"));
        Assert.Equal("Origin", response.Headers["Vary"]);
    }

    [Fact]
    public void Cors_NullResponseIsPreserved()
    {
        Assert.Null(CorsHandler.AddCorsHeaders(new APIGatewayProxyRequest(), null));
    }

    [Theory]
    [InlineData("Accept-Encoding", "Accept-Encoding, Origin")]
    [InlineData("Accept-Encoding, origin", "Accept-Encoding, origin")]
    public async Task Cors_PreservesExistingVaryWithoutDuplicateOrigin(string initial, string expected)
    {
        await CorsHandler.InitializeAsync();
        var response = new APIGatewayProxyResponse
        {
            Headers = new Dictionary<string, string> { ["vary"] = initial }
        };

        CorsHandler.AddCorsHeaders(Request("https://galashow.cloud"), response);

        Assert.Equal(expected, response.Headers.Single(h => h.Key.Equals("Vary", StringComparison.OrdinalIgnoreCase)).Value);
    }

    [Fact]
    public async Task Cors_DeniedOriginCannotInheritPermissiveUpstreamHeaders()
    {
        await CorsHandler.InitializeAsync();
        var response = new APIGatewayProxyResponse
        {
            Headers = new Dictionary<string, string>
            {
                ["access-control-allow-origin"] = "*",
                ["Access-Control-Allow-Credentials"] = "true",
                ["Content-Type"] = "application/json"
            }
        };

        CorsHandler.AddCorsHeaders(Request("https://attacker.example"), response);

        Assert.DoesNotContain(response.Headers.Keys, key => key.StartsWith("Access-Control-", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("application/json", response.Headers["Content-Type"]);
    }

    private static APIGatewayProxyRequest Request(string origin) => new()
    {
        HttpMethod = "OPTIONS",
        Headers = new Dictionary<string, string> { ["oRiGiN"] = origin }
    };
}
