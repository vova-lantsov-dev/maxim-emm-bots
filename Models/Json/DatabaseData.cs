using Newtonsoft.Json;

namespace MaximEmmBots.Models.Json
{
    internal sealed class DatabaseData
    {
        [JsonProperty(Required = Required.Always)]
        public string ConnectionString { get; set; }
    }
}