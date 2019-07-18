using System.Collections.Generic;

namespace MaximEmmBots.Services.Scheduling
{
    internal interface IWeeklyScheduler : IScheduler
    {
        IEnumerable<int> SchedulingDaysOfWeek { get; }
    }
}