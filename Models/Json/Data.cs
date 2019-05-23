using System.Collections.Generic;
using MaximEmmBots.Models.Json.DistributionBot;
using MaximEmmBots.Models.Json.GuestsBot;
using MaximEmmBots.Models.Json.ReviewBot;

namespace MaximEmmBots.Models.Json
{
    internal sealed class Data
    {
        public List<Restaurant> Restaurants { get; set; }
        
        public BotData Bot { get; set; }
        
        public string MongoConnectionString { get; set; }
        
        public GoogleCredentials GoogleCredentials { get; set; }
        
        public DistributionBotData DistributionBot { get; set; }
        
        public ReviewBotData ReviewBot { get; set; }
        
        public GuestsBotData GuestsBot { get; set; }
    }
}