using System.Text.Json;
using GalaShow.Common.Service;

namespace GalaShow.Token.Tests;

public sealed class PhaseDataRulesTests
{
    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement;

    [Theory]
    [InlineData("{\"READY\":2000,\"INPUT\":15000,\"WAIT\":-1}")]
    [InlineData("{\"READY\":0,\"CLEANUP\":1500}")]
    [InlineData("{}")]
    [InlineData("{\"phases\":[],\"totalPhases\":8,\"INPUT\":1000}")]
    public void ValidPhaseData_Passes(string json)
    {
        Assert.Null(PhaseDataRules.Validate(Json(json)));
    }

    [Fact]
    public void MissingPhaseData_Passes()
    {
        Assert.Null(PhaseDataRules.Validate(null));
        Assert.Null(PhaseDataRules.Validate(Json("null")));
    }

    [Theory]
    [InlineData("{\"WAIT\":-2}", "WAIT")]
    [InlineData("{\"INPUT\":1.5}", "INPUT")]
    [InlineData("{\"READY\":\"2000\"}", "READY")]
    [InlineData("[1,2]", "object")]
    public void InvalidPhaseData_Fails(string json, string expected)
    {
        var error = PhaseDataRules.Validate(Json(json));
        Assert.NotNull(error);
        Assert.Contains(expected, error);
    }

    [Fact]
    public void InfiniteIsMinusOne()
    {
        Assert.Equal(-1, PhaseDataRules.Infinite);
        Assert.Equal(8, PhaseDataRules.Phases.Length);
    }
}
