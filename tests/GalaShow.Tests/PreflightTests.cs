using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

namespace GalaShow.Token.Tests;

[Collection("Environment configuration")]
public sealed class PreflightTests : ConfigurationEnvironment
{
    [Theory]
    [InlineData("Token")]
    [InlineData("BackGround")]
    [InlineData("Banner")]
    [InlineData("Minigame")]
    [InlineData("Policy")]
    [InlineData("Sns")]
    public async Task Options_DoesNotRequireDatabaseOrAwsSecrets(string functionName)
    {
        Set("STAGE", "prod");
        Set("CORS_ALLOWED_ORIGINS", "https://admin.galashow.cloud");
        Func<APIGatewayProxyRequest, ILambdaContext, Task<APIGatewayProxyResponse?>> handler = functionName switch
        {
            "Token" => new GalaShow.Token.Function().FunctionHandler,
            "BackGround" => new GalaShow.BackGround.Function().FunctionHandler,
            "Banner" => new GalaShow.Banner.Function().FunctionHandler,
            "Minigame" => new GalaShow.Minigame.Function().FunctionHandler,
            "Policy" => new GalaShow.Policy.Function().FunctionHandler,
            "Sns" => new GalaShow.Sns.Function().FunctionHandler,
            _ => throw new ArgumentOutOfRangeException(nameof(functionName))
        };

        var response = await handler(new APIGatewayProxyRequest
        {
            HttpMethod = "OPTIONS",
            Path = "/preflight",
            Headers = new Dictionary<string, string>
            {
                ["Origin"] = "https://admin.galashow.cloud",
                ["Access-Control-Request-Method"] = "POST"
            }
        }, null!);

        Assert.NotNull(response);
        Assert.Equal(200, response.StatusCode);
        Assert.Equal("https://admin.galashow.cloud", response.Headers["Access-Control-Allow-Origin"]);
        Assert.Contains("POST", response.Headers["Access-Control-Allow-Methods"]);
        Assert.Equal("Origin", response.Headers["Vary"]);
    }
}
