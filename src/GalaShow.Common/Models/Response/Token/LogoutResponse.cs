using System.Text.Json.Serialization;

namespace GalaShow.Common.Models.Response.Token
{
    public class LogoutResponse
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; set; } = true;
    }
}
