using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace MaximEmmBots.Services.GuestsBot
{
    internal sealed class WorkerService : BackgroundService
    {
        private readonly GoogleSheetsService _googleSheetsService;

        public WorkerService(GoogleSheetsService googleSheetsService)
        {
            _googleSheetsService = googleSheetsService;
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _googleSheetsService.ExecuteForGuestsBotAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromMinutes(5d), stoppingToken);
            }
        }
    }
}