using Newtonsoft.Json;

namespace MaximEmmBots.Models.Json.Distribution
{
    internal sealed class DistributionData
    {
        [JsonProperty(Required = Required.Always)]
        public TimerData Timer { get; set; }
        
        [JsonProperty(Required = Required.Always)]
        public SpreadsheetData Spreadsheet { get; set; }
        
        [JsonProperty(Required = Required.Always)]
        public CultureInfoData CultureInfo { get; set; }
    }
}