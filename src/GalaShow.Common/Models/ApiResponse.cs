using System.Text.Json.Serialization;
using GalaShow.Common.Errors;

namespace GalaShow.Common.Models
{
    public class ApiResponse<T>
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("data")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public T? Data { get; set; }

        [JsonPropertyName("error")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ApiError? Error { get; set; }

        public static ApiResponse<T> Success(T data, int statusCode = 200)
            => new()
            {
                Status = statusCode.ToString(),
                Data = data
            };
        
        public static ApiResponse<object> Success(int statusCode = 200)
            => new()
            {
                Status = statusCode.ToString(),
                Data = null
            };

                public static ApiResponse<T> Fail(ErrorInfo errorInfo)
            => new()
            {
                Status = ((int)errorInfo.Code).ToString(),
                Error = new ApiError { Code = ((int)errorInfo.Code).ToString(), Message = errorInfo.Message }
            };
    }

    public class ApiError
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}
