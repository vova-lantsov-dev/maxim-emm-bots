using Microsoft.Extensions.Logging;

namespace MaximEmmBots.Extensions
{
    internal static class LoggingExtensions
    {
        internal static void Configure(ILoggingBuilder loggingBuilder)
        {
            loggingBuilder.AddConsole();
            loggingBuilder.SetMinimumLevel(LogLevel.Trace);
        }
    }
}