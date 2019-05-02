using Newtonsoft.Json;

namespace MaximEmmBots.Models.Json
{
    internal sealed class ScriptRunnerData
    {
        [JsonProperty(Required = Required.Always)]
        public string WorkingDirectory { get; set; }
        
        [JsonProperty(Required = Required.Always)]
        public string Arguments { get; set; }
        
        [JsonProperty(Required = Required.Always)]
        public string FileName { get; set; }
    }
}