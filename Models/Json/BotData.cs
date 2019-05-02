using Newtonsoft.Json;

namespace MaximEmmBots.Models.Json
{
    internal sealed class BotData
    {
        [JsonProperty(Required = Required.Always)]
        public string Token { get; set; }
        
        [JsonProperty(Required = Required.Always)]
        public string Username { get; set; }
    }
}