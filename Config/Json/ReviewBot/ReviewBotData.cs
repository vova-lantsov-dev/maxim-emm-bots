using System.Collections.Generic;

namespace MaximEmmBots.Models.Json.ReviewBot
{
    internal sealed class ReviewBotData
    {
        public ScriptData Script { get; set; }
        
        public Dictionary<string, int> MaxValuesOfRating { get; set; }
        
        public List<string> PreferAvatarOverProfileLinkFor { get; set; }
    }
}