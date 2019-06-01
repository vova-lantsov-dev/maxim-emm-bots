using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MaximEmmBots.Models.Json;
using MaximEmmBots.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MaximEmmBots.Services.DistributionBot
{
    internal sealed class WorkerService : BackgroundService
    {
        private readonly DistributionBotSheetsService _distributionBotSheetsService;
        private readonly CultureService _cultureService;
        private readonly Data _data;
        private readonly ILogger _logger;
        
        private static readonly TimeSpan Time1D = new TimeSpan(1, 0, 0, 0);
        
        public WorkerService(DistributionBotSheetsService distributionBotSheetsService,
            CultureService cultureService,
            IOptions<DataOptions> dataOptions,
            ILoggerFactory loggerFactory)
        {
            _distributionBotSheetsService = distributionBotSheetsService;
            _cultureService = cultureService;
            _data = dataOptions.Value.Data;
            _logger = loggerFactory.CreateLogger("DistributionBotWorkerService");
        }
        
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting DistributionBotWorkerService...");
            
            return Task.WhenAll(_data.Restaurants.Select(r => RunForRestaurantAsync(r, stoppingToken)));
        }

        private async Task RunForRestaurantAsync(Restaurant restaurant, CancellationToken stoppingToken)
        {
            if (!TimeSpan.TryParseExact(restaurant.DistributionBot.RunAt, "c", _cultureService.CultureFor(restaurant),
                out var runAt))
            {
                _logger.LogError("Unable to parse {0}, restaurant id is {1}",
                    restaurant.DistributionBot.RunAt, restaurant.ChatId);
                return;
            }

            TimeSpan GetStartDelay()
            {
                var currentTime = _cultureService.NowFor(restaurant).TimeOfDay;
                var startDelay = currentTime <= runAt ? runAt - currentTime : runAt + (Time1D - currentTime);
                
                _logger.LogDebug("Start delay is {0}, currentTime is {1}, restaurant id is {2}",
                    startDelay, currentTime, restaurant.ChatId);

                return startDelay;
            }
            
            await Task.Delay(GetStartDelay(), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Running for restaurant id {0}", restaurant.ChatId);
                
                await _distributionBotSheetsService.ExecuteAsync(restaurant, stoppingToken);
                await Task.Delay(GetStartDelay(), stoppingToken);
            }
            
            _logger.LogInformation("Canceled for restaurant id {0}", restaurant.ChatId);
        }
    }
}