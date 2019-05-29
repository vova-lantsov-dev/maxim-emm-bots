using System;
using System.Collections.Generic;
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
using ReviewBotWorkerService = MaximEmmBots.Services.ReviewBot.WorkerService;
using DistributionBotWorkerService = MaximEmmBots.Services.DistributionBot.WorkerService;
using GuestsBotWorkerService = MaximEmmBots.Services.GuestsBot.WorkerService;
using ChartWorkerService = MaximEmmBots.Services.Charts.WorkerService;
using Context = MaximEmmBots.Services.Context;

namespace MaximEmmBots.Extensions
{
    internal static class ServiceCollectionExtensions
    {
        internal static void AddGeneralServices(this IServiceCollection services, Data data)
        {
            services.Configure<DataOptions>(options => options.Data = data);

            services.AddSingleton<Context>();
            services.AddSingleton<CultureService>();
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
        }

        internal static void AddBotServices(this IServiceCollection services, string botToken)
        {
            services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));
            services.AddSingleton<IUpdateHandler, BotHandler>();
            services.AddHostedService<BotHandlerService>();
        }

        internal static void AddLocalizationServices(this IServiceCollection services,
            IReadOnlyDictionary<string, LocalizationModel> localizationModels)
        {
            services.AddSingleton(localizationModels);
        }

        internal static void AddGuestsBot(this IServiceCollection services)
        {
            services.AddSingleton<GuestsBotSheetsService>();
            services.AddHostedService<GuestsBotWorkerService>();
        }

        internal static void AddDistributionBot(this IServiceCollection services)
        {
            services.AddSingleton<DistributionBotSheetsService>();
            services.AddHostedService<DistributionBotWorkerService>();
        }

        internal static void AddChartServices(this IServiceCollection services)
        {
            services.AddSingleton<ChartClient>();
            services.AddHostedService<ChartWorkerService>();
            // TODO chart notifier service
            
            services.AddHttpClient<ChartClient>(chartClient =>
                {
                    chartClient.BaseAddress = new Uri("https://image-charts.com/chart");
                })
                .AddTransientHttpErrorPolicy(policyBuilder =>
                    policyBuilder.WaitAndRetryAsync(10, _ => TimeSpan.FromSeconds(10d)));
        }
    }
}