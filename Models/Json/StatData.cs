using System;

namespace MaximEmmBots.Models.Json
{
    internal sealed class StatData
    {
        public string Id { get; set; }
        
        public string SendAt { get; set; }
        
        public DayOfWeek? StartFromDayOfWeek { get; set; }
        
        public int TakeDays { get; set; }
    }
}