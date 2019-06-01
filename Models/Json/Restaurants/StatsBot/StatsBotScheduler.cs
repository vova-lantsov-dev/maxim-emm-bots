using System;

namespace MaximEmmBots.Models.Json.Restaurants.StatsBot
{
    internal sealed class StatsBotScheduler
    {
        public string Id { get; set; }
        
        public string SendAt { get; set; }
        
        public DayOfWeek? StartFromDayOfWeek { get; set; }
        
        public int TakeDays { get; set; }
    }
}