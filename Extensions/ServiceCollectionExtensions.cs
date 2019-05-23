using System;
using System.Collections.Generic;
using System.Globalization;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using MaximEmmBots.Models.Json;
using MaximEmmBots.Options;
using Microsoft.Extensions.DependencyInjection;
using MaximEmmBots.Services;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using ReviewBotWorkerService = MaximEmmBots.Services.ReviewBot.WorkerService;
using DistributionBotWorkerService = MaximEmmBots.Services.DistributionBot.WorkerService;

namespace MaximEmmBots.Extensions
{
    internal static class ServiceCollectionExtensions
    {
        internal static void AddGeneralServices(this IServiceCollection services, Data data)
        {
            services.Configure<DataOptions>(options => options.Data = data);

            services.AddSingleton<Context>();
        }

        internal static void AddGoogleServices(this IServiceCollection services,
            BaseClientService.Initializer googleInitializer)
        {
            services.AddSingleton(new SheetsService(googleInitializer));
            services.AddSingleton<GoogleSheetsService>();
        }

        internal static void AddWorkerServices(this IServiceCollection services)
        {
            services.AddHostedService<ReviewBotWorkerService>();
            services.AddHostedService<DistributionBotWorkerService>();
        }

        internal static void AddBotServices(this IServiceCollection services, string botToken)
        {
            services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));
            services.AddSingleton<IUpdateHandler, BotHandler>();
            services.AddHostedService<BotHandlerService>();
        }

        internal static void AddLocalizationServices(this IServiceCollection services,
            IReadOnlyDictionary<string, LocalizationModel> localizationModels,
            IReadOnlyDictionary<string, TimeZoneInfo> timeZoneDictionary,
            IReadOnlyDictionary<string, CultureInfo> cultureDictionary)
        {
            services.AddSingleton(localizationModels);
            services.AddSingleton(timeZoneDictionary);
            services.AddSingleton(cultureDictionary);
        }
    }
}