using System.Collections.Generic;
using MaximEmmBots.Models.Json.Distribution;
using MaximEmmBots.Models.Json.ReviewBot;

namespace MaximEmmBots.Models.Json
{
    internal sealed class Data
    {
        public List<Restaurant> Restaurants { get; set; }
        
        public BotData Bot { get; set; }
        
        public DatabaseData Database { get; set; }
        
        public DistributionData Distribution { get; set; }
        
        public ReviewBotData ReviewBot { get; set; }
    }
}