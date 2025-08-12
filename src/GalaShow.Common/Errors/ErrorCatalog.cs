using System.Collections.Concurrent;

namespace GalaShow.Common.Errors
{
    public static class ErrorCatalog
    {
        private static readonly ConcurrentDictionary<ErrorCode, ErrorInfo> Map = new()
        {
            [ErrorCode.BadRequest]           = new(ErrorCode.BadRequest, "Bad request", 400),
            [ErrorCode.Unauthorized]         = new(ErrorCode.Unauthorized, "Unauthorized", 401),
            [ErrorCode.Forbidden]            = new(ErrorCode.Forbidden, "Forbidden", 403),
            [ErrorCode.PathNotFound]             = new(ErrorCode.PathNotFound, "Path Not found", 404),
            [ErrorCode.Conflict]             = new(ErrorCode.Conflict, "Conflict", 409),
            [ErrorCode.TooManyRequests]      = new(ErrorCode.TooManyRequests, "Too many requests", 429),
            [ErrorCode.Internal]             = new(ErrorCode.Internal, "Internal server error", 500),
            [ErrorCode.ServiceUnavailable]   = new(ErrorCode.ServiceUnavailable, "Service unavailable", 503),

            [ErrorCode.AuthInvalidCredentials] = new(ErrorCode.AuthInvalidCredentials, "Invalid credentials", 401),
            [ErrorCode.AuthTokenMissing]       = new(ErrorCode.AuthTokenMissing, "Missing access token", 401),
            [ErrorCode.AuthTokenInvalid]       = new(ErrorCode.AuthTokenInvalid, "Invalid access token", 401),
            [ErrorCode.AuthTokenExpired]       = new(ErrorCode.AuthTokenExpired, "Access token expired", 401, AddWwwAuthenticateHeader: true),
            [ErrorCode.AuthRefreshInvalid]     = new(ErrorCode.AuthRefreshInvalid, "Invalid refresh token", 401),
            [ErrorCode.AuthRefreshExpired]     = new(ErrorCode.AuthRefreshExpired, "Refresh token expired", 401),
            [ErrorCode.AuthRefreshRevoked]     = new(ErrorCode.AuthRefreshRevoked, "Refresh token revoked", 401),

            [ErrorCode.BannerNotFound]         = new(ErrorCode.BannerNotFound, "Banner not found", 404),
            [ErrorCode.BannerUpdateFailed]     = new(ErrorCode.BannerUpdateFailed, "Failed to update banner", 500),

            [ErrorCode.BackgroundNotFound]         = new(ErrorCode.BackgroundNotFound, "Background not found", 404),
            [ErrorCode.BackgroundUpdateFailed]     = new(ErrorCode.BackgroundUpdateFailed, "Failed to update Background", 500),
            
            [ErrorCode.PolicyNotFound]         = new(ErrorCode.PolicyNotFound, "Policy not found", 404),
            [ErrorCode.PolicyUpdateFailed]     = new(ErrorCode.PolicyUpdateFailed, "Failed to update Policy", 500),
        };

        public static ErrorInfo Get(ErrorCode code, string? msgOverride = null)
        {
            var info = Map.TryGetValue(code, out var e)
                ? e
                : new ErrorInfo(ErrorCode.Unknown, "Unknown error", 500);

            return msgOverride is null ? info : info with { Message = msgOverride };
        }
    }
}
