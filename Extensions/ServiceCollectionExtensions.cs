using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using MaximEmmBots.Models.Json;
using MaximEmmBots.Options;
using Microsoft.Extensions.DependencyInjection;
using MaximEmmBots.Services;
using Telegram.Bot;
using ReviewBotWorkerService = MaximEmmBots.Services.ReviewGrabberBot.WorkerService;
using DistributionBotWorkerService = MaximEmmBots.Services.DistributionBot.WorkerService;

namespace MaximEmmBots.Extensions
{
    internal static class ServiceCollectionExtensions
    {
        internal static void AddGeneralServices(this IServiceCollection services, Data data,
            BaseClientService.Initializer googleInitializer)
        {
            services.Configure<DataOptions>(options => options.Data = data);

            services.AddSingleton(new TelegramBotClient(data.Bot.Token));
            services.AddSingleton<Context>();
            services.AddSingleton(new SheetsService(googleInitializer));
            services.AddSingleton<DistributionService>();
            services.AddSingleton<BotHandler>();
        }

        internal static void AddWorkerServices(this IServiceCollection services)
        {
            services.AddHostedService<ReviewBotWorkerService>();
            services.AddHostedService<DistributionBotWorkerService>();
            services.AddHostedService<BotHandlerService>();
        }
    }
}