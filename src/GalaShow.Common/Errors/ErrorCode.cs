namespace GalaShow.Common.Errors
{
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

        // Authentication Errors (starting from 600 to avoid conflict with standard HTTP codes)
        AuthInvalidCredentials = 600,
        AuthTokenMissing = 601,
        AuthTokenInvalid = 602,
        AuthTokenExpired = 603,
        AuthRefreshInvalid = 604,
        AuthRefreshExpired = 605,
        AuthRefreshRevoked = 606,

        BannerNotFound = 610,
        BannerUpdateFailed = 611,
        
        BackgroundNotFound = 620,
        BackgroundUpdateFailed = 621,
        
        PolicyNotFound = 630,
        PolicyUpdateFailed = 631,
        
        SnsLinkNotFound = 640,
        SnsLinkUpdateFailed = 641,
    }
}