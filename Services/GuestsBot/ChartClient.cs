using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using MaximEmmBots.Models.Charts;

// ReSharper disable PossibleMultipleEnumeration

namespace MaximEmmBots.Services.GuestsBot
{
    internal sealed class ChartClient
    {
        private const string BaseAddress = "https://image-charts.com/chart";
        
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
            var uriBuilder = new UriBuilder(BaseAddress);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);

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

            query["chs"] = "600x600";
            query["cht"] = "pd";
            query["chd"] = $"t:{string.Join(',', chd)}";
            query["chdl"] = string.Join('|', chdl);
            query["chli"] = chli.ToString();
            query["chl"] = string.Join('|', chl);
            uriBuilder.Query = query.ToString();
            
            await using var respStream = await _client.GetStreamAsync(uriBuilder.Uri);
            await respStream.CopyToAsync(destinationStream);
            destinationStream.Position = 0L;
        }
    }
}