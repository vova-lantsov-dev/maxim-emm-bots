using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MaximEmmBots.Extensions;
using MaximEmmBots.Models.Charts;
using MaximEmmBots.Models.Json;
using MaximEmmBots.Models.Json.Restaurants;
using MaximEmmBots.Models.Json.Restaurants.StatsBot;
using MaximEmmBots.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable AccessToModifiedClosure
// ReSharper disable ImplicitlyCapturedClosure

namespace MaximEmmBots.Services.StatsBot
{
    internal sealed class WorkerService : BackgroundService
    {
        private readonly Data _data;
        private readonly Context _context;
        private readonly CultureService _cultureService;
        private readonly ILogger _logger;
        private readonly ChartClient _chartClient;
        private readonly ITelegramBotClient _client;

        public WorkerService(IOptions<DataOptions> dataOptions,
            Context context,
            CultureService cultureService,
            ILoggerFactory loggerFactory,
            ChartClient chartClient,
            ITelegramBotClient client)
        {
            _context = context;
            _cultureService = cultureService;
            _chartClient = chartClient;
            _client = client;
            _logger = loggerFactory.CreateLogger("ChartsWorkerService");
            _data = dataOptions.Value.Data;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.WhenAll(_data.Restaurants.SelectMany(restaurant =>
                restaurant.StatsBot.Schedulers.Select(stat => RunChartHandlerAsync(restaurant, stat, stoppingToken))));
        }

        private async Task RunChartHandlerAsync(Restaurant restaurant, StatsBotScheduler statScheduler, CancellationToken stoppingToken)
        {
            if (!TimeSpan.TryParseExact(statScheduler.SendAt, "c", CultureExtensions.DefaultCulture, TimeSpanStyles.None,
                out var sendAt))
            {
                _logger.LogError("SendAt parameter has incorrect format");
                return;
            }

            if (sendAt < TimeSpan.Zero || sendAt > new TimeSpan(23, 59, 59))
            {
                _logger.LogError("SendAt is lower than 00:00:00 or greater than 23:59:59");
                return;
            }

            if (statScheduler.TakeDays < 1)
            {
                _logger.LogError("TakeData is lower than 1");
                return;
            }

            var takeDays = new TimeSpan(statScheduler.TakeDays, 0, 0, 0);
            _logger.LogDebug("TakeDays is {0}", statScheduler.TakeDays);
            
            var lastStat = await _context.SentStats
                .Find(ss => ss.StatId == statScheduler.Id)
                .SortByDescending(ss => ss.SentDate)
                .Project(ss => ss.SentDate)
                .FirstOrDefaultAsync(stoppingToken);
            _logger.LogDebug("LastStat is {0}", lastStat);

            if (lastStat == default)
            {
                _logger.LogInformation("lastStat is default, processing expected value");
                
                var now = _cultureService.NowFor(restaurant);
                _logger.LogDebug("now is {0}", now);
                
                lastStat = now.Date.Subtract(takeDays);
                _logger.LogDebug("lastStat, subtracted from now, is {0}", lastStat);

                if (now.TimeOfDay > sendAt)
                {
                    _logger.LogInformation("now.TimeOfDay > sendAt", now.TimeOfDay, sendAt);
                    lastStat = lastStat.AddDays(1d);
                    _logger.LogDebug("lastStat incremented by 1 day: {0}", lastStat);
                }

                if (statScheduler.StartFromDayOfWeek.HasValue && lastStat.DayOfWeek != statScheduler.StartFromDayOfWeek.Value)
                {
                    _logger.LogInformation("StartFromDayOfWeek has value. {0} != {1}",
                        lastStat.DayOfWeek, statScheduler.StartFromDayOfWeek.Value);
                    
                    var time1Day = TimeSpan.FromDays(1d);
                    while (lastStat.DayOfWeek != statScheduler.StartFromDayOfWeek.Value)
                        lastStat = lastStat.Add(time1Day);
                    
                    _logger.LogDebug("lastStat updated to be {0}. Now its value is {1}",
                        statScheduler.StartFromDayOfWeek.Value, lastStat);
                }
            }
            
            TimeSpan GetDelayTime()
            {
                var expectedDate = lastStat.Add(takeDays + sendAt);
                var delayTimeTemp = expectedDate - _cultureService.NowFor(restaurant);
                
                _logger.LogDebug("Delay time is {0}", delayTimeTemp);

                return delayTimeTemp;
            }

            await Task.Delay(GetDelayTime(), stoppingToken);

            var culture = _cultureService.CultureFor(restaurant);

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = _cultureService.NowFor(restaurant);
                var statsForPeriod = await _context.SentForms
                    .Find(sf => sf.Date >= lastStat && sf.Date <= now.Date && sf.RestaurantId == restaurant.ChatId)
                    .ToListAsync(stoppingToken);

                var pieChartDictionary = new Dictionary<string, int>();
                foreach (var stat in statsForPeriod)
                {
                    var isFirstDay = stat.Date == now.Date;
                    var isLastDay = stat.Date == lastStat;
                    
                    foreach (var statItem in stat.Items)
                    {
                        if (isFirstDay && statItem.SentTime < sendAt || isLastDay && statItem.SentTime > now.TimeOfDay)
                            continue;

                        pieChartDictionary.TryGetValue(statItem.EmployeeName, out var employeeWeight);
                        pieChartDictionary[statItem.EmployeeName] = employeeWeight + 1;
                    }
                }

                var pieChartItems = new List<PieChartItem>();
                foreach (var (employeeName, weight) in pieChartDictionary)
                {
                    pieChartItems.Add(new PieChartItem(weight, employeeName));
                }

                await using (var ms = new MemoryStream())
                {
                    await _chartClient.LoadDoughnutPieChartAsync(ms, pieChartItems);
                    var model = _cultureService.ModelFor(restaurant);
                    await _client.SendPhotoAsync(restaurant.ChatId, ms,
                        string.Format(model.StatsForPeriod, (lastStat + sendAt).ToString("G", culture),
                            now.ToString("G", culture)), ParseMode.Html, cancellationToken: stoppingToken);
                }

                lastStat = now.Date;
                
                await Task.Delay(GetDelayTime(), stoppingToken);
            }
        }
    }
}