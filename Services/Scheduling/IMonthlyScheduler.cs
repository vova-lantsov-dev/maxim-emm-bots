using System.Collections.Generic;

namespace MaximEmmBots.Services.Scheduling
{
    internal interface IMonthlyScheduler : IScheduler
    {
        IEnumerable<int> SchedulingDaysOfMonth { get; }
    }
}