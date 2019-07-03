using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MaximEmmBots.Services.Scheduling
{
    internal sealed class SchedulingService : IHostedService
    {
        private readonly CancellationToken _stoppingToken;
        private readonly CultureService _cultureService;
        private readonly List<Task> _runningTasks = new List<Task>();
        private readonly ILogger<SchedulingService> _logger;

        public SchedulingService(IHostApplicationLifetime lifetime,
            CultureService cultureService,
            IEnumerable<IScheduler> schedulers,
            ILogger<SchedulingService> logger)
        {
            _cultureService = cultureService;
            _logger = logger;
            _stoppingToken = lifetime.ApplicationStopping;
            
            _runningTasks.AddRange(schedulers.Select(RunSchedulerAsync));
        }

        private async Task RunSchedulerAsync(IScheduler scheduler)
        {
            var getSchedulingDelay = scheduler.SchedulingMode switch
            {
                SchedulingMode.Daily => (Func<IScheduler, TimeSpan>) GetDailyDelay,
                SchedulingMode.Weekly => (Func<IScheduler, TimeSpan>) GetWeeklyDelay,
                SchedulingMode.Monthly => (Func<IScheduler, TimeSpan>) GetMonthlyDelay,
                SchedulingMode.Static => (Func<IScheduler, TimeSpan>) (s => s.SchedulingTime),
                _ => throw new ArgumentOutOfRangeException(nameof(scheduler.SchedulingMode))
            };

            while (!_stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(getSchedulingDelay(scheduler), _stoppingToken);
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
                    await scheduler.OnElapseAsync(_stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while running scheduler called {0}", scheduler.SchedulerName);
                }
            }
        }

        private TimeSpan GetDailyDelay(IScheduler scheduler)
        {
            var now = _cultureService.NowFor(scheduler.Restaurant);
            return now.TimeOfDay <= scheduler.SchedulingTime
                ? scheduler.SchedulingTime - now.TimeOfDay
                : TimeSpan.FromDays(1d) + scheduler.SchedulingTime - now.TimeOfDay;
        }

        private TimeSpan GetWeeklyDelay(IScheduler scheduler)
        {
            if (!(scheduler is IWeeklyScheduler weeklyScheduler))
                throw new ArgumentException("Weekly scheduler is not of type IWeeklyScheduler", nameof(scheduler));

            var now = _cultureService.NowFor(scheduler.Restaurant);
            var nowDayOfWeek = now.DayOfWeek == DayOfWeek.Sunday ? 7 : (int) now.DayOfWeek;
            if (scheduler.SchedulingTime > now.TimeOfDay)
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

            return (now.TimeOfDay <= scheduler.SchedulingTime
                       ? scheduler.SchedulingTime - now.TimeOfDay
                       : TimeSpan.FromDays(1d) + scheduler.SchedulingTime - now.TimeOfDay) +
                   TimeSpan.FromDays(daysDelay);
        }

        private TimeSpan GetMonthlyDelay(IScheduler scheduler)
        {
            if (!(scheduler is IMonthlyScheduler monthlyScheduler))
                throw new ArgumentException("Monthly scheduler is not of type IMonthlyScheduler", nameof(scheduler));

            var now = _cultureService.NowFor(scheduler.Restaurant);
            var next = now.AddMonths(1);
            var nowDayOfMonth = now.Day;
            var nextDaysOfMonth = DateTime.DaysInMonth(next.Year, next.Month);
            if (scheduler.SchedulingTime > now.TimeOfDay)
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

            return (now.TimeOfDay <= scheduler.SchedulingTime
                       ? scheduler.SchedulingTime - now.TimeOfDay
                       : TimeSpan.FromDays(1d) + scheduler.SchedulingTime - now.TimeOfDay) +
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