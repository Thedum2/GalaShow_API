namespace GalaShow.Common.Errors
{
    public sealed record ErrorInfo(
        ErrorCode Code,
        string Message,
        bool AddWwwAuthenticateHeader = false
    );
}