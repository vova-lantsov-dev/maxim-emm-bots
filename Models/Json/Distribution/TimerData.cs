using Newtonsoft.Json;

namespace MaximEmmBots.Models.Json.Distribution
{
    internal sealed class TimerData
    {
        [JsonProperty(Required = Required.Always)]
        public double StartTimeInMinutes { get; set; }
        
        [JsonProperty(Required = Required.Always)]
        public double ElapseIntervalInMinutes { get; set; }
    }
}