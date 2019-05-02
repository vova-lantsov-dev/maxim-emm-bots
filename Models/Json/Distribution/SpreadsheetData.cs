using Newtonsoft.Json;

namespace MaximEmmBots.Models.Json.Distribution
{
    internal sealed class SpreadsheetData
    {
        [JsonProperty(Required = Required.Always)]
        public string Id { get; set; }
        
        [JsonProperty("client_id", Required = Required.Always)]
        public string ClientId { get; set; }
        
        [JsonProperty("client_secret", Required = Required.Always)]
        public string ClientSecret { get; set; }
    }
}