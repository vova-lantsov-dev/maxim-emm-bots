using System.Collections.Generic;

namespace MaximEmmBots.Models.Json
{
    internal sealed class Restaurant
    {
        public string Name { get; set; }

        public long ChatId { get; set; }

        public List<int> AdminIds { get; set; }
        
        public string PlaceId { get; set; }
        
        public string PlaceInfo { get; set; }
        
        public CultureData Culture { get; set; }

        public Dictionary<string, string> Urls { get; set; }
    }
}