using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace MaximEmmBots.Services.DistributionBot
{
    internal sealed class WorkerService : BackgroundService
    {
        private readonly GoogleSheetsService _googleSheetsService;
        
        private static readonly TimeSpan Time1D = new TimeSpan(1, 0, 0, 0);
        private static readonly TimeSpan Time20H = new TimeSpan(20, 0, 0);
        
        private static TimeSpan StartDelay
        {
            get
            {
                var currentTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, GoogleSheetsService.ZoneInfo).TimeOfDay;
                return currentTime <= Time20H ? Time20H - currentTime : Time20H + (Time1D - currentTime);
            }
        }
        
        public WorkerService(GoogleSheetsService googleSheetsService)
        {
            _googleSheetsService = googleSheetsService;
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(StartDelay, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await _googleSheetsService.ExecuteAsync(stoppingToken);
                await Task.Delay(StartDelay, stoppingToken);
            }
        }
    }
}