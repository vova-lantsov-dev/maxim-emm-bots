using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using MaximEmmBots.Models.Json;
using MaximEmmBots.Options;
using MaximEmmBots.Services;
using MaximEmmBots.Services.Charts;
using MaximEmmBots.Services.DistributionBot;
using MaximEmmBots.Services.GuestsBot;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Context = MaximEmmBots.Services.Context;
using ReviewBotWorkerService = MaximEmmBots.Services.ReviewBot.WorkerService;
using DistributionBotWorkerService = MaximEmmBots.Services.DistributionBot.WorkerService;
using GuestsBotWorkerService = MaximEmmBots.Services.GuestsBot.WorkerService;

namespace MaximEmmBots.Extensions
{
    internal static class ServiceCollectionExtensions
    {
        internal static void AddGeneralServices(this IServiceCollection services, Data data)
        {
            services.Configure<DataOptions>(options => options.Data = data);

            services.AddSingleton<Context>();
            services.AddSingleton<HttpClient>(); // TODO redundant
            services.AddSingleton<ChartClient>();
        }

        internal static void AddGoogleServices(this IServiceCollection services,
            BaseClientService.Initializer googleInitializer)
        {
            services.AddSingleton(new SheetsService(googleInitializer));
            services.AddSingleton<GoogleSheetsService>();
            services.AddSingleton<GuestsBotSheetsService>();
            services.AddSingleton<DistributionBotSheetsService>();
        }

        internal static void AddWorkerServices(this IServiceCollection services)
        {
            //services.AddHostedService<ReviewBotWorkerService>();
            //services.AddHostedService<DistributionBotWorkerService>();
            services.AddHostedService<GuestsBotWorkerService>();
        }

        internal static void AddBotServices(this IServiceCollection services, string botToken)
        {
            services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));
            services.AddSingleton<IUpdateHandler, BotHandler>();
            //services.AddHostedService<BotHandlerService>();
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

        internal static void AddHttpFactory(this IServiceCollection services)
        {
            services.AddHttpClient<ChartClient>()
                .AddTransientHttpErrorPolicy(policyBuilder =>
                    policyBuilder.WaitAndRetryAsync(10, _ => TimeSpan.FromSeconds(10d)));
        }
    }
}