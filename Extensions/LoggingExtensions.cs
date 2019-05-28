using System.Threading;
using MaximEmmBots.Models.Json;
using Microsoft.Extensions.Logging;

namespace MaximEmmBots.Extensions
{
    internal static class LoggingExtensions
    {
        internal static void Configure(ILoggingBuilder loggingBuilder)
        {
            loggingBuilder.AddConsole();
            loggingBuilder.SetMinimumLevel(LogLevel.Information);
        }

        internal static EventId GetEventId(Restaurant restaurant, StatData statData)
        {
            return new EventId(Thread.CurrentThread.ManagedThreadId, $"{restaurant.ChatId}-{statData.Id}");
        }
    }
}