using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using MaximEmmBots.Models.Charts;

// ReSharper disable PossibleMultipleEnumeration

namespace MaximEmmBots.Services.Charts
{
    internal sealed class ChartClient
    {
        private readonly HttpClient _client;

        public ChartClient(HttpClient client)
        {
            _client = client;
        }

        /// <summary>
        /// chs - chart size;
        /// cht - chart type;
        /// chd - chart data (percentage);
        /// chdl - chart legends;
        /// chli - chart middle label;
        /// chl - chart labels.
        /// </summary>
        internal async Task LoadDoughnutPieChartAsync(Stream destinationStream, ICollection<PieChartItem> items)
        {
            var chli = 0;
            var chd = new int[items.Count];
            var chdl = new string[items.Count];
            var chl = new string[items.Count];

            {
                var itemsIndex = 0;
                foreach (var item in items)
                {
                    chd[itemsIndex] = item.Weight;
                    chli += item.Weight;
                    chdl[itemsIndex] = item.Legend;
                    chl[itemsIndex++] = item.Text;
                }
            }

            var query = new Dictionary<string, string>
            {
                ["chs"] = "600x600",
                ["cht"] = "pd",
                ["chd"] = $"t:{string.Join(',', chd)}",
                ["chdl"] = string.Join('|', chdl),
                ["chli"] = chli.ToString(),
                ["chl"] = string.Join('|', chl)
            };
            var url = "chart?" + string.Join('&', query.Select(it => $"{it.Key}={it.Value}"));
            
            await using var respStream = await _client.GetStreamAsync(url);
            await respStream.CopyToAsync(destinationStream);
            destinationStream.Position = 0L;
        }
    }
}