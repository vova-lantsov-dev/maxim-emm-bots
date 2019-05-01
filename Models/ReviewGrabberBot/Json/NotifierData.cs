using System.Collections.Generic;
using Newtonsoft.Json;

namespace MaximEmmBots.Models.ReviewGrabberBot.Json
{
    internal sealed class NotifierData
    {
        [JsonProperty(Required = Required.Always)]
        public List<Restaurant> Restaurants { get; set; }
        
        [JsonProperty(Required = Required.Always)]
        public Dictionary<string, int> MaxValuesOfRating { get; set; }

        [JsonProperty(Required = Required.Always)]
        public List<string> PreferAvatarOverProfileLinkFor { get; set; }
    }
}