using System.Collections.Generic;
using MaximEmmBots.Models.Json.DistributionBot;

namespace MaximEmmBots.Models.Json
{
    internal sealed class Restaurant
    {
        public string Name { get; set; }

        public long ChatId { get; set; }

        public List<int> AdminIds { get; set; }
        
        public char PlaceId { get; set; }
        
        public string PlaceInfo { get; set; }
        
        public CultureData Culture { get; set; }
        
        public GuestsBotData GuestsBot { get; set; }
        
        public DistributionBotData DistributionBot { get; set; }

        public Dictionary<string, string> Urls { get; set; }
    }
}