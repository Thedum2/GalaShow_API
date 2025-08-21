using System.Text.Json.Serialization;

namespace GalaShow.Common.Models.Request.Sns
{
    public class UpdateSnsLinksRequest
    {
        [JsonPropertyName("data")]
        public List<SnsLinkItem> Data { get; set; } = new();

        public class SnsLinkItem
        {
            [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
            [JsonPropertyName("url")]   public string Url   { get; set; } = string.Empty;
            [JsonPropertyName("icon_url")]  public string IconUrl  { get; set; } = string.Empty;
            [JsonPropertyName("order")] public int Order    { get; set; }
        }
    }
}