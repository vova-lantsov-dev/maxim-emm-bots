using Newtonsoft.Json;

namespace MaximEmmBots.Models.Json.ReviewBot
{
    internal sealed class ScriptData
    {
        [JsonProperty(Required = Required.Always)]
        public string WorkingDirectory { get; set; }
        
        [JsonProperty(Required = Required.Always)]
        public string Arguments { get; set; }
        
        [JsonProperty(Required = Required.Always)]
        public string FileName { get; set; }
    }
}