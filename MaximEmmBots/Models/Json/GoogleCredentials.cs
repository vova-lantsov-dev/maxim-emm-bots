using System.Text.Json.Serialization;

namespace MaximEmmBots.Models.Json
{
    internal sealed class GoogleCredentials
    {
        [JsonPropertyName("client_id")]
        public string ClientId { get; set; }
        
        [JsonPropertyName("client_secret")]
        public string ClientSecret { get; set; }
    }
}