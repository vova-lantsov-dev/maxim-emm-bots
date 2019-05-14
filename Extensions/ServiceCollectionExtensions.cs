using MaximEmmBots.Models.Json;
using MaximEmmBots.Options;
using Microsoft.Extensions.DependencyInjection;
using MaximEmmBots.Services;
using MaximEmmBots.Services.ReviewGrabberBot;
using Telegram.Bot;

namespace MaximEmmBots.Extensions
{
    internal static class ServiceCollectionExtensions
    {   
        internal static void AddServices(this IServiceCollection services, Data data)
        {
            services.Configure<DataOptions>(options => options.Data = data);

            services.AddSingleton(new TelegramBotClient(data.Bot.Token));
            services.AddSingleton<Context>();
            
            services.AddSingleton<BotHandler>();
            services.AddHostedService<BotHandlerService>();

            services.AddHostedService<WorkerService>();
        }
    }
}