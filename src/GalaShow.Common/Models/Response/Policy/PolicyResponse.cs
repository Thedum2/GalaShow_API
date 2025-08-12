using System.Text.Json.Serialization;

namespace GalaShow.Common.Models.Response.Policy
{
    public class PolicyResponse
    {
        [JsonPropertyName("termsOfService")]
        public string TermsOfService { get; set; } = string.Empty;

        [JsonPropertyName("privacyPolicy")]
        public string PrivacyPolicy { get; set; } = string.Empty;
    }
}