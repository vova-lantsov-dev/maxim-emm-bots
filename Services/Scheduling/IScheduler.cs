using System;
using System.Threading;
using System.Threading.Tasks;
using MaximEmmBots.Models.Json.Restaurants;

namespace MaximEmmBots.Services.Scheduling
{
    internal interface IScheduler
    {
        string SchedulerName { get; }
        
        Restaurant Restaurant { get; set; }
        
        SchedulingMode SchedulingMode { get; }
        
        TimeSpan SchedulingTime { get; }

        Task OnElapseAsync(CancellationToken cancellationToken);
    }
}