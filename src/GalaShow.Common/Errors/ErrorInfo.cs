namespace GalaShow.Common.Errors
{
    public sealed record ErrorInfo(
        ErrorCode Code,
        string Message,
        int HttpStatus,
        bool AddWwwAuthenticateHeader = false
    );
}