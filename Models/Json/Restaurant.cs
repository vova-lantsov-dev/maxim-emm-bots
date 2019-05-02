using System.Collections.Generic;
using Newtonsoft.Json;

namespace MaximEmmBots.Models.Json
{
    internal sealed class Restaurant
    {
        [JsonProperty(Required = Required.Always)]
        public string Name { get; set; }

        [JsonProperty(Required = Required.Always)]
        public long ChatId { get; set; }

        [JsonProperty(Required = Required.Always)]
        public List<int> AdminIds { get; set; }
        
        [JsonProperty(Required = Required.Always)]
        public string PlaceId { get; set; }
        
        [JsonProperty(Required = Required.Always)]
        public string PlaceText { get; set; }

        [JsonProperty(Required = Required.Always)]
        public Dictionary<string, string> Urls { get; set; }
    }
}