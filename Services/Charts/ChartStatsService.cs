using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MaximEmmBots.Extensions;
using MaximEmmBots.Models.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

// ReSharper disable ImplicitlyCapturedClosure

namespace MaximEmmBots.Services.Charts
{
    internal sealed class ChartStatsService : BackgroundService
    {
        private readonly Data _data;
        private readonly Context _context;
        private readonly CultureService _cultureService;
        private readonly ILogger<ChartStatsService> _logger;

        public ChartStatsService(IOptions<Data> dataOptions,
            Context context,
            CultureService cultureService,
            ILogger<ChartStatsService> logger)
        {
            _context = context;
            _cultureService = cultureService;
            _logger = logger;
            _data = dataOptions.Value;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.WhenAll(_data.Restaurants.SelectMany(restaurant =>
                restaurant.GuestsBot.Stats.Select(stat => RunChartHandlerAsync(restaurant, stat, stoppingToken))));
        }

        private async Task RunChartHandlerAsync(Restaurant restaurant, StatData statData, CancellationToken stoppingToken)
        {
            if (!TimeSpan.TryParseExact(statData.SendAt, "c", CultureExtensions.DefaultCulture, TimeSpanStyles.None,
                out var sendAt))
            {
                _logger.LogError(LoggingExtensions.GetEventId(restaurant, statData),
                    "SendAt parameter has incorrect format");
                return;
            }

            if (sendAt < TimeSpan.Zero || sendAt > new TimeSpan(23, 59, 59))
            {
                _logger.LogError(LoggingExtensions.GetEventId(restaurant, statData),
                    "SendAt is lower than 00:00:00 or greater than 23:59:59");
                return;
            }

            if (statData.TakeDays < 1 || statData.TakeDays > 7)
            {
                _logger.LogError(LoggingExtensions.GetEventId(restaurant, statData),
                    "TakeData is lower than 1 or greater than 7");
                return;
            }

            var takeDays = new TimeSpan(statData.TakeDays, 0, 0, 0);
            _logger.LogDebug(LoggingExtensions.GetEventId(restaurant, statData),
                "TakeDays is {0}", statData.TakeDays);
            
            var lastStat = await _context.SentStats
                .Find(ss => ss.StatId == statData.Id)
                .SortByDescending(ss => ss.SentDate)
                .Project(ss => ss.SentDate)
                .FirstOrDefaultAsync(stoppingToken);
            _logger.LogDebug(LoggingExtensions.GetEventId(restaurant, statData),
                "LastStat is {0}", lastStat);

            if (lastStat == default)
            {
                _logger.LogInformation(LoggingExtensions.GetEventId(restaurant, statData),
                    "lastStat is default, processing expected value");
                
                var now = _cultureService.NowFor(restaurant);
                _logger.LogDebug(LoggingExtensions.GetEventId(restaurant, statData), "now is {0}", now);
                
                lastStat = now.Date.Subtract(takeDays);
                _logger.LogDebug(LoggingExtensions.GetEventId(restaurant, statData),
                    "lastStat, subtracted from now, is {0}", lastStat);

                if (now.TimeOfDay > sendAt)
                {
                    _logger.LogInformation(LoggingExtensions.GetEventId(restaurant, statData),
                        "now.TimeOfDay > sendAt", now.TimeOfDay, sendAt);
                    lastStat = lastStat.AddDays(1d);
                    _logger.LogDebug(LoggingExtensions.GetEventId(restaurant, statData),
                        "lastStat incremented by 1 day: {0}", lastStat);
                }

                if (statData.StartFromDayOfWeek.HasValue && lastStat.DayOfWeek != statData.StartFromDayOfWeek.Value)
                {
                    _logger.LogInformation(LoggingExtensions.GetEventId(restaurant, statData),
                        "StartFromDayOfWeek has value. {0} != {1}",
                        lastStat.DayOfWeek, statData.StartFromDayOfWeek.Value);
                    
                    var time1Day = TimeSpan.FromDays(1d);
                    while (lastStat.DayOfWeek != statData.StartFromDayOfWeek.Value)
                        lastStat = lastStat.Add(time1Day);
                    
                    _logger.LogDebug(LoggingExtensions.GetEventId(restaurant, statData),
                        "lastStat updated to be {0}. Now its value is {1}",
                        statData.StartFromDayOfWeek.Value, lastStat);
                }
            }
            
            TimeSpan GetDelayTime()
            {
                var expectedDate = lastStat.Add(takeDays + sendAt);
                var delayTimeTemp = expectedDate - _cultureService.NowFor(restaurant);
                
                _logger.LogDebug(LoggingExtensions.GetEventId(restaurant, statData),
                    "Delay time is {0}", delayTimeTemp);

                return delayTimeTemp;
            }

            await Task.Delay(GetDelayTime(), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                // TODO stats processing
                
                await Task.Delay(GetDelayTime(), stoppingToken);
            }
        }
    }
}