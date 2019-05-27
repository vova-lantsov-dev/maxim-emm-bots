using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Microsoft.Extensions.Logging;

namespace MaximEmmBots.Services
{
    internal sealed class GoogleSheetsService
    {
        private readonly SheetsService _sheetsService;
        private readonly ILogger<GoogleSheetsService> _logger;

        public GoogleSheetsService(SheetsService sheetsService, ILogger<GoogleSheetsService> logger)
        {
            _sheetsService = sheetsService;
            _logger = logger;
        }
        
        internal async Task<ValueRange> GetValueRangeAsync(string sId, string range, CancellationToken stoppingToken)
        {
            _logger.LogDebug("SpreadsheetId is {0}", sId);
            _logger.LogDebug("Range is {0}", range);
            
            var request = _sheetsService.Spreadsheets.Values.Get(sId, range);

            try
            {
                return await request.ExecuteAsync(stoppingToken);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Error occurred while executing spreadsheet request.");
                return null;
            }
        }
    }
}