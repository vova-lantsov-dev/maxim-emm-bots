using System.Text.Json.Serialization;

namespace Host.Options
{
    public sealed class GoogleOptions
    {
        [JsonPropertyName("client_id")]
        public string ClientId { get; set; }
        [JsonPropertyName("client_secret")]
        public string ClientSecret { get; set; }
    }
}