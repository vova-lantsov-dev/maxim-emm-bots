using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace MaximEmmBots.Services
{
    internal sealed class GoogleSheetsService
    {
        private readonly SheetsService _sheetsService;

        public GoogleSheetsService(SheetsService sheetsService)
        {
            _sheetsService = sheetsService;
        }
        
        internal async Task<ValueRange> GetValueRangeAsync(string sId, string range, CancellationToken stoppingToken)
        {
            var request = _sheetsService.Spreadsheets.Values.Get(sId, range);

            try
            {
                return await request.ExecuteAsync(stoppingToken);
            }
            catch (Exception e) when (!(e is OperationCanceledException))
            {
                return null;
            }
        }
    }
}