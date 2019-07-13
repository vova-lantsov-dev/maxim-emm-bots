using System;
using System.Threading;
using System.Threading.Tasks;
using MaximEmmBots.Models.Json.Restaurants;
using MaximEmmBots.Services.Scheduling;

namespace MaximEmmBots.Services.DistributionBot
{
    internal sealed class DistributionBotScheduler : IScheduler
    {
        private readonly DistributionBotSheetsService _distributionBotSheetsService;
        private readonly CultureService _cultureService;
        
        public DistributionBotScheduler(
            DistributionBotSheetsService distributionBotSheetsService,
            CultureService cultureService)
        {
            _distributionBotSheetsService = distributionBotSheetsService;
            _cultureService = cultureService;
        }

        public string SchedulerName => "DistributionBotScheduler";
        
        public SchedulingMode SchedulingMode => SchedulingMode.Daily;
        
        public Func<Restaurant, TimeSpan> SchedulingTime => r => TimeSpan.ParseExact(r.DistributionBot.RunAt, "c",
            _cultureService.CultureFor(r));
        
        public Task OnElapseAsync(Restaurant restaurant, CancellationToken cancellationToken) =>
            _distributionBotSheetsService.ExecuteAsync(restaurant, cancellationToken);
    }
}