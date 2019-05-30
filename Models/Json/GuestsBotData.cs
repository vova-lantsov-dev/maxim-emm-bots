using System.Collections.Generic;

namespace MaximEmmBots.Models.Json
{
    internal sealed class GuestsBotData
    {
        public string SpreadsheetId { get; set; }
        
        public string TableName { get; set; }
        
        public int ColumnOfName { get; set; }
        
        public List<StatData> Stats { get; set; }
    }
}