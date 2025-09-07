namespace GalaShow.Common.Errors
{
    public enum ErrorCode
    {
        // Standard HTTP Errors (using actual HTTP status codes)
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

        // Banner Errors (starting from 610)
        BannerNotFound = 610,
        BannerUpdateFailed = 611,
        
        // Background Errors (starting from 620)
        BackgroundNotFound = 620,
        BackgroundUpdateFailed = 621,
        
        // Policy Errors (starting from 630)
        PolicyNotFound = 630,
        PolicyUpdateFailed = 631,
        
        // SnsLink Errors (starting from 640)
        SnsLinkNotFound = 640,
        SnsLinkUpdateFailed = 641,

        // Question Categories (starting from 650)
        QuestionCategoryNotFound = 650,
        QuestionCategoryCreateFailed = 651,
        QuestionCategoryUpdateFailed = 652,
        QuestionCategoryDeleteFailed = 653,

        // Questions (starting from 660)
        QuestionNotFound = 660,
        QuestionCreateFailed = 661,
        QuestionUpdateFailed = 662,
        QuestionDeleteFailed = 663,
    }
}