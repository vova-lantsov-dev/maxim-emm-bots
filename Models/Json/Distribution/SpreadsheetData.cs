using System.Text.Json.Serialization;

namespace MaximEmmBots.Models.Json.Distribution
{
    internal sealed class SpreadsheetData
    {
        public string Id { get; set; }
        
        [JsonPropertyName("client_id")]
        public string ClientId { get; set; }
        
        [JsonPropertyName("client_secret")]
        public string ClientSecret { get; set; }
    }
}