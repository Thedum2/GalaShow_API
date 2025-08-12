using System.Text.Json.Serialization;

namespace GalaShow.Common.Models.Request.Policy
{
    public class UpdatePolicyRequest
    {
        [JsonPropertyName("termsOfService")]
        public string TermsOfService { get; set; } = string.Empty;

        [JsonPropertyName("privacyPolicy")]
        public string PrivacyPolicy { get; set; } = string.Empty;
    }
}