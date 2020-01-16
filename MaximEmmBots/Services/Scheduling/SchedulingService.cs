using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MaximEmmBots.Models.Json.Restaurants;
using MaximEmmBots.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MaximEmmBots.Services.Scheduling
{
    internal sealed class SchedulingService : IHostedService
    {
        private readonly CancellationToken _stoppingToken;
        private readonly CultureService _cultureService;
        private readonly IEnumerable<Task> _runningTasks;
        private readonly ILogger<SchedulingService> _logger;

        public SchedulingService(IHostApplicationLifetime lifetime,
            CultureService cultureService,
            IEnumerable<IScheduler> schedulers,
            ILogger<SchedulingService> logger,
            IOptions<DataOptions> dataOptions)
        {
            _cultureService = cultureService;
            _logger = logger;
            _stoppingToken = lifetime.ApplicationStopping;
            
            _runningTasks = dataOptions.Value.Data.Restaurants.SelectMany(r =>
                schedulers.Where(s => s.SchedulingTime(r) != Timeout.InfiniteTimeSpan)
                    .Select(s => RunSchedulerAsync(s, r))).ToArray();
        }

        private async Task RunSchedulerAsync(IScheduler scheduler, Restaurant restaurant)
        {
            var getSchedulingDelay = scheduler.SchedulingMode switch
            {
                SchedulingMode.Daily => (Func<IScheduler, Restaurant, TimeSpan>) GetDailyDelay,
                SchedulingMode.Weekly => (Func<IScheduler, Restaurant, TimeSpan>) GetWeeklyDelay,
                SchedulingMode.Monthly => (Func<IScheduler, Restaurant, TimeSpan>) GetMonthlyDelay,
                SchedulingMode.Static => (Func<IScheduler, Restaurant, TimeSpan>)((s, _) => s.SchedulingTime(restaurant)),
                _ => throw new ArgumentOutOfRangeException(nameof(scheduler.SchedulingMode))
            };

            while (!_stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(getSchedulingDelay(scheduler, restaurant), _stoppingToken).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    // silent mode
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Error occurred while delaying scheduler called {0}",
                        scheduler.SchedulerName);
                    break;
                }

                if (_stoppingToken.IsCancellationRequested)
                    break;

                try
                {
                    await scheduler.OnElapseAsync(restaurant, _stoppingToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while running scheduler called {0}", scheduler.SchedulerName);
                }
            }
        }

        private TimeSpan GetDailyDelay(IScheduler scheduler, Restaurant restaurant)
        {
            var now = _cultureService.NowFor(restaurant);
            return now.TimeOfDay <= scheduler.SchedulingTime(restaurant)
                ? scheduler.SchedulingTime(restaurant) - now.TimeOfDay
                : TimeSpan.FromDays(1d) + scheduler.SchedulingTime(restaurant) - now.TimeOfDay;
        }

        private TimeSpan GetWeeklyDelay(IScheduler scheduler, Restaurant restaurant)
        {
            if (!(scheduler is IWeeklyScheduler weeklyScheduler))
                throw new ArgumentException("Weekly scheduler is not of type IWeeklyScheduler", nameof(scheduler));

            var now = _cultureService.NowFor(restaurant);
            var nowDayOfWeek = now.DayOfWeek == DayOfWeek.Sunday ? 7 : (int) now.DayOfWeek;
            if (scheduler.SchedulingTime(restaurant) > now.TimeOfDay)
                nowDayOfWeek++;

            var daysDelay = 7;
            foreach (var schedulingDayOfWeek in weeklyScheduler.SchedulingDaysOfWeek)
            {
                var leftDays = schedulingDayOfWeek == nowDayOfWeek
                    ? 0
                    : schedulingDayOfWeek > nowDayOfWeek
                        ? schedulingDayOfWeek - nowDayOfWeek
                        : schedulingDayOfWeek + 7 - nowDayOfWeek;
                if (daysDelay > leftDays)
                    daysDelay = leftDays;

                if (daysDelay == 0)
                    break;
            }

            return (now.TimeOfDay <= scheduler.SchedulingTime(restaurant)
                       ? scheduler.SchedulingTime(restaurant) - now.TimeOfDay
                       : TimeSpan.FromDays(1d) + scheduler.SchedulingTime(restaurant) - now.TimeOfDay) +
                   TimeSpan.FromDays(daysDelay);
        }

        private TimeSpan GetMonthlyDelay(IScheduler scheduler, Restaurant restaurant)
        {
            if (!(scheduler is IMonthlyScheduler monthlyScheduler))
                throw new ArgumentException("Monthly scheduler is not of type IMonthlyScheduler", nameof(scheduler));

            var now = _cultureService.NowFor(restaurant);
            var next = now.AddMonths(1);
            var nowDayOfMonth = now.Day;
            var nextDaysOfMonth = DateTime.DaysInMonth(next.Year, next.Month);
            if (scheduler.SchedulingTime(restaurant) > now.TimeOfDay)
                nowDayOfMonth++;

            var daysDelay = 32;
            foreach (var schedulingDayOfMonth in monthlyScheduler.SchedulingDaysOfMonth)
            {
                var leftDays = schedulingDayOfMonth == nowDayOfMonth
                    ? 0
                    : schedulingDayOfMonth > nowDayOfMonth
                        ? schedulingDayOfMonth - nowDayOfMonth
                        : schedulingDayOfMonth + nextDaysOfMonth - nowDayOfMonth;
                if (daysDelay > leftDays)
                    daysDelay = leftDays;

                if (daysDelay == 0)
                    break;
            }

            return (now.TimeOfDay <= scheduler.SchedulingTime(restaurant)
                       ? scheduler.SchedulingTime(restaurant) - now.TimeOfDay
                       : TimeSpan.FromDays(1d) + scheduler.SchedulingTime(restaurant) - now.TimeOfDay) +
                   TimeSpan.FromDays(daysDelay);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.WhenAll(_runningTasks);
        }
    }
}