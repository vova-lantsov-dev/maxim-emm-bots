using System;
using System.Threading;
using System.Threading.Tasks;
using MaximEmmBots.Models.Json.Restaurants;

namespace MaximEmmBots.Services.Scheduling
{
    internal interface IScheduler
    {
        string SchedulerName { get; }
        
        SchedulingMode SchedulingMode { get; }
        
        Func<Restaurant, TimeSpan> SchedulingTime { get; }

        Task OnElapseAsync(Restaurant restaurant, CancellationToken cancellationToken);
    }
}