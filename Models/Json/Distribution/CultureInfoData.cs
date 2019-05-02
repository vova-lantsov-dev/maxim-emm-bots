using Newtonsoft.Json;

namespace MaximEmmBots.Models.Json.Distribution
{
    internal sealed class CultureInfoData
    {
        [JsonProperty(Required = Required.Always)]
        public string Name { get; set; }
        
        [JsonProperty(Required = Required.Always)]
        public string DateFormat { get; set; }
        
        [JsonProperty(Required = Required.Always)]
        public string TimeZoneInfo { get; set; }
    }
}