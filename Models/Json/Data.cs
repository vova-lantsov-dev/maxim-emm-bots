using System.Collections.Generic;
using MaximEmmBots.Models.Json.Restaurants;
using MaximEmmBots.Models.Json.ReviewBot;

namespace MaximEmmBots.Models.Json
{
    internal sealed class Data
    {
        public List<Restaurant> Restaurants { get; set; }
        
        public BotData Bot { get; set; }
        
        public string MongoConnectionString { get; set; }
        
        public GoogleCredentials GoogleCredentials { get; set; }
        
        public ReviewBotData ReviewBot { get; set; }
    }
}