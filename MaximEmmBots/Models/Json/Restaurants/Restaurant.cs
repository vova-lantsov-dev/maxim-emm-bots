using System.Collections.Generic;
using MaximEmmBots.Models.Json.Restaurants.DistributionBot;
using MaximEmmBots.Models.Json.Restaurants.GuestsBot;
using MaximEmmBots.Models.Json.Restaurants.StatsBot;

namespace MaximEmmBots.Models.Json.Restaurants
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
        
        public StatsBotData StatsBot { get; set; }

        public Dictionary<string, string> Urls { get; set; }
    }
}