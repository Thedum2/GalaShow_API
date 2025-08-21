using System.Text.Json.Serialization;

namespace GalaShow.Common.Models.Response.Sns
{
    public class SnsLinkResponse
    {
        [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
        [JsonPropertyName("url")]   public string Url   { get; set; } = string.Empty;
        [JsonPropertyName("icon_url")]  public string IconUrl  { get; set; } = string.Empty;
        [JsonPropertyName("order")] public int Order    { get; set; }
    }
}