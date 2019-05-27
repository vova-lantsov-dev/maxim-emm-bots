using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using MaximEmmBots.Models.Charts;

// ReSharper disable PossibleMultipleEnumeration

namespace MaximEmmBots.Services.GuestsBot
{
    internal sealed class ChartsService
    {
        private const string BaseAddress = "https://image-charts.com/chart";
        
        private readonly HttpClient _client;

        public ChartsService(HttpClient client)
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
        internal async Task LoadDoughnutPieChartAsync(Stream destinationStream, IEnumerable<PieChartItem> items)
        {
            var uriBuilder = new UriBuilder(BaseAddress);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            query["chs"] = "600x600";
            query["cht"] = "pd";
            query["chd"] = $"t:{string.Join(',', items.Select(it => it.Weight))}";
            query["chdl"] = string.Join('|', items.Select(it => it.Legend));
            query["chli"] = items.Sum(it => it.Weight).ToString();
            query["chl"] = string.Join('|', items.Select(it => it.Text));
            uriBuilder.Query = query.ToString();
            await using var respStream = await _client.GetStreamAsync(uriBuilder.Uri);
            await respStream.CopyToAsync(destinationStream);
            destinationStream.Position = 0L;
        }
    }
}