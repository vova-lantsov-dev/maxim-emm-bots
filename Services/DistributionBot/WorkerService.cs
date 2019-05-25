using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MaximEmmBots.Models.Json;
using MaximEmmBots.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace MaximEmmBots.Services.DistributionBot
{
    internal sealed class WorkerService : BackgroundService
    {
        private readonly GoogleSheetsService _googleSheetsService;
        private readonly IReadOnlyDictionary<string, TimeZoneInfo> _timeZones;
        private readonly Data _data;
        
        private static readonly TimeSpan Time1D = new TimeSpan(1, 0, 0, 0);
        private static readonly TimeSpan Time20H = new TimeSpan(20, 0, 0);
        
        public WorkerService(GoogleSheetsService googleSheetsService, IReadOnlyDictionary<string, TimeZoneInfo> timeZones,
            IOptions<DataOptions> dataOptions)
        {
            _googleSheetsService = googleSheetsService;
            _timeZones = timeZones;
            _data = dataOptions.Value.Data;
        }
        
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.WhenAll(_data.Restaurants.Select(r => RunForRestaurantAsync(r, stoppingToken)));
        }

        private async Task RunForRestaurantAsync(Restaurant restaurant, CancellationToken stoppingToken)
        {
            await Task.Delay(GetStartDelay(), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await _googleSheetsService.ExecuteForDistributionBotAsync(restaurant.Culture.TimeZone,
                    restaurant.Culture.Name, stoppingToken);
                await Task.Delay(GetStartDelay(), stoppingToken);
            }

            TimeSpan GetStartDelay()
            {
                var currentTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, _timeZones[restaurant.Culture.TimeZone]).TimeOfDay;
                return currentTime <= Time20H ? Time20H - currentTime : Time20H + (Time1D - currentTime);
            }
        }
    }
}