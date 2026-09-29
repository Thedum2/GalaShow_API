using Amazon.Lambda.APIGatewayEvents;
using GalaShow.Common.Service;

namespace GalaShow.Token.Tests;

[Collection("Environment configuration")]
public sealed class StageResolverTests : ConfigurationEnvironment
{
    [Theory]
    [InlineData("prod", Stage.Prod)]
    [InlineData(" Production ", Stage.Prod)]
    [InlineData("dev", Stage.Dev)]
    [InlineData("DEVELOPMENT", Stage.Dev)]
    public void Resolve_DeploymentStageOverridesRequestAndLocalFlag(string configured, Stage expected)
    {
        Set("STAGE", configured);
        Set("AWS_SAM_LOCAL", "true");
        var request = Request("api-dev.galashow.cloud", "dev");

        Assert.Equal(expected, StageResolver.Resolve(request));
    }

    [Theory]
    [InlineData("staging")]
    [InlineData(" ")]
    public void Resolve_InvalidExplicitStageFailsInsteadOfEnablingDevelopment(string configured)
    {
        Set("STAGE", configured);

        var error = Assert.Throws<InvalidOperationException>(() =>
            StageResolver.Resolve(Request("api-dev.galashow.cloud", "dev")));

        Assert.Contains("STAGE", error.Message);
    }

    [Fact]
    public void Resolve_SamLocalDefaultsToDevelopment()
    {
        Set("AWS_SAM_LOCAL", "true");

        Assert.Equal(Stage.Dev, StageResolver.Resolve());
        Assert.True(StageResolver.IsDev());
    }

    [Theory]
    [InlineData("dev", Stage.Dev)]
    [InlineData("prod", Stage.Prod)]
    public void Resolve_ValidGatewayStageOverridesHost(string gatewayStage, Stage expected)
    {
        Assert.Equal(expected, StageResolver.Resolve(Request("api-dev.galashow.cloud", gatewayStage)));
    }

    [Theory]
    [InlineData("api-dev.galashow.cloud", Stage.Dev)]
    [InlineData("API-DEV.GALASHOW.CLOUD", Stage.Dev)]
    [InlineData("api.galashow.cloud", Stage.Prod)]
    [InlineData("api-dev.galashow.cloud.attacker.example", Stage.Prod)]
    [InlineData("other-api-dev.galashow.cloud", Stage.Prod)]
    [InlineData("api-dev.attacker.example", Stage.Prod)]
    [InlineData("api-dev.galashow.xyz", Stage.Prod)]
    [InlineData("localhost", Stage.Prod)]
    [InlineData("", Stage.Prod)]
    public void Resolve_OnlyExactCloudHostsSelectAStage(string host, Stage expected)
    {
        Assert.Equal(expected, StageResolver.Resolve(Request(host, "$default")));
    }

    [Fact]
    public void Resolve_UnknownDeploymentFailsClosedWithoutFunctionNameGuessing()
    {
        Set("AWS_LAMBDA_FUNCTION_NAME", "unrelated-device-service");

        Assert.Equal(Stage.Prod, StageResolver.Resolve(new APIGatewayProxyRequest()));
        Assert.True(StageResolver.IsProd());
    }

    [Fact]
    public void Resolve_ReevaluatesDeploymentStageInsteadOfReusingAnEarlierRequest()
    {
        Assert.Equal(Stage.Dev, StageResolver.Resolve(Request("api-dev.galashow.cloud")));
        Set("STAGE", "prod");

        Assert.Equal(Stage.Prod, StageResolver.Resolve(Request("api-dev.galashow.cloud")));
        Assert.False(StageResolver.IsDev());
    }

    [Fact]
    public void Resolve_UnknownRequestCannotReuseDevelopmentFromAnEarlierRequest()
    {
        StageResolver.Resolve(Request("api-dev.galashow.cloud"));

        Assert.Equal(Stage.Prod, StageResolver.Resolve(new APIGatewayProxyRequest()));
    }

    [Fact]
    public async Task Production_RejectsDevelopmentCredentialsEvenWithADevelopmentHost()
    {
        Set("STAGE", "prod");
        StageResolver.Resolve(Request("api-dev.galashow.cloud", "dev"));

        var (accepted, _) = await TokenService.Instance.ValidateCredentialAsync("dev", "dev");

        Assert.False(accepted);
    }

    private static APIGatewayProxyRequest Request(string host, string? stage = null) => new()
    {
        Headers = new Dictionary<string, string> { ["hOsT"] = host },
        RequestContext = new APIGatewayProxyRequest.ProxyRequestContext { Stage = stage }
    };
}
