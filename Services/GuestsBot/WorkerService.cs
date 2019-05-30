using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MaximEmmBots.Services.GuestsBot
{
    internal sealed class WorkerService : BackgroundService
    {
        private readonly GuestsBotSheetsService _guestsBotSheetsService;
        private readonly ILogger _logger;

        public WorkerService(GuestsBotSheetsService guestsBotSheetsService,
            ILoggerFactory loggerFactory)
        {
            _guestsBotSheetsService = guestsBotSheetsService;
            _logger = loggerFactory.CreateLogger("GuestsBotWorkerService");
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting GuestsBotWorkerService...");
            
            while (!stoppingToken.IsCancellationRequested)
            {
                await _guestsBotSheetsService.ExecuteAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromMinutes(5d), stoppingToken);
            }
        }
    }
}