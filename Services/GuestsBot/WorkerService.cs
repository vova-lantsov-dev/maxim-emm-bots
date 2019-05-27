using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace MaximEmmBots.Services.GuestsBot
{
    internal sealed class WorkerService : BackgroundService
    {
        private readonly GuestsBotSheetsService _guestsBotSheetsService;

        public WorkerService(GuestsBotSheetsService guestsBotSheetsService)
        {
            _guestsBotSheetsService = guestsBotSheetsService;
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _guestsBotSheetsService.ExecuteAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromMinutes(5d), stoppingToken);
            }
        }
    }
}