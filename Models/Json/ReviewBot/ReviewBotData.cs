using System.Collections.Generic;
using Newtonsoft.Json;

namespace MaximEmmBots.Models.Json.ReviewBot
{
    internal sealed class ReviewBotData
    {
        [JsonProperty(Required = Required.Always)]
        public ScriptData Script { get; set; }
        
        [JsonProperty(Required = Required.Always)]
        public Dictionary<string, int> MaxValuesOfRating { get; set; }
        
        [JsonProperty(Required = Required.Always)]
        public List<string> PreferAvatarOverProfileLinkFor { get; set; }
    }
}