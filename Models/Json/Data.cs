using System.Collections.Generic;
using MaximEmmBots.Models.Json.Distribution;
using MaximEmmBots.Models.Json.ReviewBot;
using Newtonsoft.Json;

namespace MaximEmmBots.Models.Json
{
    internal sealed class Data
    {
        [JsonProperty(Required = Required.Always)]
        public List<Restaurant> Restaurants { get; set; }
        
        [JsonProperty(Required = Required.Always)]
        public BotData Bot { get; set; }
        
        [JsonProperty(Required = Required.Always)]
        public DatabaseData Database { get; set; }
        
        [JsonProperty(Required = Required.Always)]
        public DistributionData Distribution { get; set; }
        
        [JsonProperty(Required = Required.Always)]
        public ReviewBotData ReviewBot { get; set; }
    }
}