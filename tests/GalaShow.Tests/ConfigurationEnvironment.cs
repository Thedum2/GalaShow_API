namespace GalaShow.Token.Tests;

[CollectionDefinition("Environment configuration", DisableParallelization = true)]
public sealed class ConfigurationEnvironmentCollection;

public abstract class ConfigurationEnvironment : IDisposable
{
    private static readonly string[] VariableNames =
    [
        "STAGE", "AWS_SAM_LOCAL", "ASPNETCORE_ENVIRONMENT", "DOTNET_ENVIRONMENT",
        "AWS_LAMBDA_FUNCTION_NAME", "DB_HOST", "DB_PORT", "DB_NAME", "DB_SECRET_ARN",
        "JWT_SECRET_ARN", "JWT_ISSUER", "JWT_AUDIENCE", "CORS_ALLOWED_ORIGINS"
    ];

    private readonly Dictionary<string, string?> _originalValues =
        VariableNames.ToDictionary(name => name, Environment.GetEnvironmentVariable);

    protected ConfigurationEnvironment()
    {
        foreach (var name in VariableNames)
            Environment.SetEnvironmentVariable(name, null);
        StageResolver.Invalidate();
    }

    protected static void Set(string name, string? value) => Environment.SetEnvironmentVariable(name, value);

    public void Dispose()
    {
        foreach (var (name, value) in _originalValues)
            Environment.SetEnvironmentVariable(name, value);
        StageResolver.Invalidate();
        GC.SuppressFinalize(this);
    }
}
