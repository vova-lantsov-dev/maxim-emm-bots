using System.Collections.Generic;

namespace MaximEmmBots.Models.Json
{
    internal sealed class NotifierData
    {
        public List<Restaurant> Restaurants { get; set; }
        
        public Dictionary<string, int> MaxValuesOfRating { get; set; }

        public List<string> PreferAvatarOverProfileLinkFor { get; set; }
    }
}