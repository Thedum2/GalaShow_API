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
            [ErrorCode.PathNotFound]             = new(ErrorCode.PathNotFound, "Path Not found"),
            [ErrorCode.Conflict]             = new(ErrorCode.Conflict, "Conflict"),
            [ErrorCode.TooManyRequests]      = new(ErrorCode.TooManyRequests, "Too many requests"),
            [ErrorCode.Internal]             = new(ErrorCode.Internal, "Internal server error"),
            [ErrorCode.ServiceUnavailable]   = new(ErrorCode.ServiceUnavailable, "Service unavailable"),

            [ErrorCode.AuthInvalidCredentials] = new(ErrorCode.AuthInvalidCredentials, "Invalid credentials"),
            [ErrorCode.AuthTokenMissing]       = new(ErrorCode.AuthTokenMissing, "Missing access token"),
            [ErrorCode.AuthTokenInvalid]       = new(ErrorCode.AuthTokenInvalid, "Invalid access token"),
            [ErrorCode.AuthTokenExpired]       = new(ErrorCode.AuthTokenExpired, "Access token expired", AddWwwAuthenticateHeader: true),
            [ErrorCode.AuthRefreshInvalid]     = new(ErrorCode.AuthRefreshInvalid, "Invalid refresh token"),
            [ErrorCode.AuthRefreshExpired]     = new(ErrorCode.AuthRefreshExpired, "Refresh token expired"),
            [ErrorCode.AuthRefreshRevoked]     = new(ErrorCode.AuthRefreshRevoked, "Refresh token revoked"),

            [ErrorCode.BannerNotFound]         = new(ErrorCode.BannerNotFound, "Banner not found"),
            [ErrorCode.BannerUpdateFailed]     = new(ErrorCode.BannerUpdateFailed, "Failed to update banner"),

            [ErrorCode.BackgroundNotFound]         = new(ErrorCode.BackgroundNotFound, "Background not found"),
            [ErrorCode.BackgroundUpdateFailed]     = new(ErrorCode.BackgroundUpdateFailed, "Failed to update Background"),
            
            [ErrorCode.PolicyNotFound]         = new(ErrorCode.PolicyNotFound, "Policy not found"),
            [ErrorCode.PolicyUpdateFailed]     = new(ErrorCode.PolicyUpdateFailed, "Failed to update Policy"),
            
            [ErrorCode.SnsLinkNotFound]         = new(ErrorCode.PolicyNotFound, "SnsLinkNotFound not found"),
            [ErrorCode.SnsLinkUpdateFailed]     = new(ErrorCode.PolicyUpdateFailed, "Failed to update SnsLinkNotFound"),

            [ErrorCode.QuestionCategoryNotFound]    = new(ErrorCode.QuestionCategoryNotFound, "Question category not found"),
            [ErrorCode.QuestionCategoryCreateFailed] = new(ErrorCode.QuestionCategoryCreateFailed, "Failed to create question category"),
            [ErrorCode.QuestionCategoryUpdateFailed] = new(ErrorCode.QuestionCategoryUpdateFailed, "Failed to update question category"),
            [ErrorCode.QuestionCategoryDeleteFailed] = new(ErrorCode.QuestionCategoryDeleteFailed, "Failed to delete question category"),

            [ErrorCode.QuestionNotFound]    = new(ErrorCode.QuestionNotFound, "Question not found"),
            [ErrorCode.QuestionCreateFailed] = new(ErrorCode.QuestionCreateFailed, "Failed to create question"),
            [ErrorCode.QuestionUpdateFailed] = new(ErrorCode.QuestionUpdateFailed, "Failed to update question"),
            [ErrorCode.QuestionDeleteFailed] = new(ErrorCode.QuestionDeleteFailed, "Failed to delete question"),
            
            [ErrorCode.InvalidChoiceCount] = new(ErrorCode.InvalidChoiceCount, "Choices must be between 2 and 4."),
        };

        public static ErrorInfo Get(ErrorCode code, string? msgOverride = null)
        {
            var info = Map.TryGetValue(code, out var e)
                ? e
                : new ErrorInfo(ErrorCode.Unknown, "Unknown error");

            return msgOverride is null ? info : info with { Message = msgOverride };
        }
    }
}
