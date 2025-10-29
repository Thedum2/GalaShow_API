namespace GalaShow.Common.Errors
{
    /// <summary>
    /// Error codes that also serve as HTTP status codes.
    /// Each error has a unique code in the 400-599 range.
    /// </summary>
    public enum ErrorCode
    {
        Unknown = 0, // Keep 0 for unknown
        BadRequest = 400,
        Unauthorized = 401,
        Forbidden = 403,
        PathNotFound = 404,
        Conflict = 409,
        TooManyRequests = 429,
        Internal = 500,
        ServiceUnavailable = 503,

        // Authentication Errors (440-449 range)
        AuthInvalidCredentials = 440,
        AuthTokenMissing = 441,
        AuthTokenInvalid = 442,
        AuthTokenExpired = 443,
        AuthRefreshInvalid = 444,
        AuthRefreshExpired = 445,
        AuthRefreshRevoked = 446,

        // Resource Not Found Errors (450-459 range)
        BannerNotFound = 450,
        BackgroundNotFound = 451,
        PolicyNotFound = 452,
        SnsLinkNotFound = 453,

        // Resource Update Failed Errors (540-549 range)
        BannerUpdateFailed = 540,
        BackgroundUpdateFailed = 541,
        PolicyUpdateFailed = 542,
        SnsLinkUpdateFailed = 543,
    }
}