namespace GalaShow.Common.Errors
{
    // 도메인별 번호대 예시: 1xxx 공통, 2xxx 인증, 3xxx 배너
    public enum ErrorCode
    {
        // Common (1xxx)
        Unknown              = 1000,
        BadRequest           = 1001,
        Unauthorized         = 1002,
        Forbidden            = 1003,
        PathNotFound             = 1004,
        Conflict             = 1009,
        TooManyRequests      = 1010,
        Internal             = 1500,
        ServiceUnavailable   = 1503,

        // Auth (2xxx)
        AuthInvalidCredentials = 2001,
        AuthTokenMissing       = 2002,
        AuthTokenInvalid       = 2003,
        AuthTokenExpired       = 2004,
        AuthRefreshInvalid     = 2010,
        AuthRefreshExpired     = 2011,
        AuthRefreshRevoked     = 2012,

        // Banner (3xxx)
        BannerNotFound       = 3004,
        BannerUpdateFailed   = 3005,
        
        // Background (4xxx)
        BackgroundNotFound       = 4004,
        BackgroundUpdateFailed   = 4005,
        
        // Policy (5xxx)
        PolicyNotFound       = 5004,
        PolicyUpdateFailed   = 5005,
        
    }
}