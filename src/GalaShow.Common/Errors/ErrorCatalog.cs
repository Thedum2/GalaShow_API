using System.Collections.Concurrent;

namespace GalaShow.Common.Errors
{
    public static class ErrorCatalog
    {
        private static readonly ConcurrentDictionary<ErrorCode, ErrorInfo> Map = new()
        {
            [ErrorCode.BadRequest]           = new(ErrorCode.BadRequest, "Bad request"),
            [ErrorCode.Unauthorized]         = new(ErrorCode.Unauthorized, "Unauthorized"),
            [ErrorCode.Forbidden]            = new(ErrorCode.Forbidden, "Forbidden"),
            [ErrorCode.Conflict]             = new(ErrorCode.Conflict, "Conflict"),
            [ErrorCode.TooManyRequests]      = new(ErrorCode.TooManyRequests, "Too many requests"),
            [ErrorCode.Internal]             = new(ErrorCode.Internal, "Internal server error"),
            [ErrorCode.ServiceUnavailable]   = new(ErrorCode.ServiceUnavailable, "Service unavailable"),

            // Auth Errors
            [ErrorCode.AuthInvalidCredentials] = new(ErrorCode.AuthInvalidCredentials, "Invalid credentials"),
            [ErrorCode.AuthTokenMissing]       = new(ErrorCode.AuthTokenMissing, "Missing access token"),
            [ErrorCode.AuthTokenInvalid]       = new(ErrorCode.AuthTokenInvalid, "Invalid access token"),
            [ErrorCode.AuthTokenExpired]       = new(ErrorCode.AuthTokenExpired, "Access token expired"),
            [ErrorCode.AuthRefreshInvalid]     = new(ErrorCode.AuthRefreshInvalid, "Invalid refresh token"),
            [ErrorCode.AuthRefreshExpired]     = new(ErrorCode.AuthRefreshExpired, "Refresh token expired"),
            [ErrorCode.AuthRefreshRevoked]     = new(ErrorCode.AuthRefreshRevoked, "Refresh token revoked"),

            // Resource Not Found Errors
            [ErrorCode.BannerNotFound]       = new(ErrorCode.BannerNotFound, "Banner not found"),
            [ErrorCode.BackgroundNotFound]   = new(ErrorCode.BackgroundNotFound, "Background not found"),
            [ErrorCode.PolicyNotFound]       = new(ErrorCode.PolicyNotFound, "Policy not found"),
            [ErrorCode.SnsLinkNotFound]      = new(ErrorCode.SnsLinkNotFound, "SnsLink not found"),

            // Resource Update Failed Errors
            [ErrorCode.BannerUpdateFailed]       = new(ErrorCode.BannerUpdateFailed, "Failed to update banner"),
            [ErrorCode.BackgroundUpdateFailed]   = new(ErrorCode.BackgroundUpdateFailed, "Failed to update background"),
            [ErrorCode.PolicyUpdateFailed]       = new(ErrorCode.PolicyUpdateFailed, "Failed to update policy"),
            [ErrorCode.SnsLinkUpdateFailed]      = new(ErrorCode.SnsLinkUpdateFailed, "Failed to update SnsLink"),
        };

        public static ErrorInfo Get(ErrorCode code, string? msgOverride = null)
        {
            var info = Map.TryGetValue(code, out var e)
                ? e
                : new ErrorInfo(ErrorCode.Internal, "Unknown error");

            return msgOverride is null ? info : info with { Message = msgOverride };
        }
    }
}
