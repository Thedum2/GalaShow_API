using System.Text.Json.Serialization;

namespace GalaShow.Common.Models
{
    public class ApiResponse<T>
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "200";

        [JsonPropertyName("data")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public T? Data { get; set; }

        [JsonPropertyName("message")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Message { get; set; }

        [JsonPropertyName("errors")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? Errors { get; set; }

        public static ApiResponse<T> SuccessResult(T data, string? message = null, string status = "200")
            => new() 
            { 
                Success = true, 
                Data = data, 
                Message = message, 
                Status = status 
            };
        
        public static ApiResponse<T> ErrorResult(string message, List<string>? errors = null, string status = "400")
            => new() 
            { 
                Success = false, 
                Message = message, 
                Errors = errors, 
                Status = status 
            };

        public static ApiResponse<T> NotFoundResult(string message = "Resource not found")
            => new() 
            { 
                Success = false, 
                Message = message, 
                Status = "404" 
            };

        public static ApiResponse<T> ServerErrorResult(string message = "Internal server error")
            => new() 
            { 
                Success = false, 
                Message = message, 
                Status = "500" 
            };
    }
}