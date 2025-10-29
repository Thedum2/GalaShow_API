namespace GalaShow.Common.Errors
{
    public sealed record ErrorInfo(
        ErrorCode Code,
        string Message,
        int HttpStatusCode,
        bool AddWwwAuthenticateHeader = false
    );
}