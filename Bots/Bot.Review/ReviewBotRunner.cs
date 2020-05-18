/*using System;
using System.Threading;
using Bot.Abstractions;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;

namespace Bot.Review
{
    public sealed class ReviewBotRunner : IBotRunner
    {
        private readonly IServiceProvider _serviceProvider;

        public ReviewBotRunner(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        
        public void Run()
        {
            var worker = ActivatorUtilities.CreateInstance<ReviewBotWorker>(_serviceProvider);
            
            RecurringJob.AddOrUpdate("review-bot", () => worker.RunAsync(CancellationToken.None), "0 #1#10 * ? * *");
        }
    }
}*/