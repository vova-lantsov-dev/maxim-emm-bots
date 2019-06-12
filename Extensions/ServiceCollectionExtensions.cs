using System;
using System.Collections.Generic;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using MaximEmmBots.Models.Json;
using MaximEmmBots.Options;
using MaximEmmBots.Services;
using MaximEmmBots.Services.DistributionBot;
using MaximEmmBots.Services.GuestsBot;
using MaximEmmBots.Services.MailBot;
using MaximEmmBots.Services.StatsBot;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Context = MaximEmmBots.Services.Context;
using ReviewBotWorkerService = MaximEmmBots.Services.ReviewBot.WorkerService;
using DistributionBotWorkerService = MaximEmmBots.Services.DistributionBot.WorkerService;
using GuestsBotWorkerService = MaximEmmBots.Services.GuestsBot.WorkerService;
using StatsBotWorkerService = MaximEmmBots.Services.StatsBot.WorkerService;

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
            services.AddSingleton(new GmailService(googleInitializer));
            services.AddSingleton<GoogleSheetsService>();
        }

        internal static void AddBotServices(this IServiceCollection services, string botToken)
        {
            services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));
            services.AddSingleton<IUpdateHandler, BotHandler>();
            services.AddHostedService<BotHandlerService>();
            
            services.AddHttpClient<IUpdateHandler, BotHandler>()
                .AddTransientHttpErrorPolicy(policyBuilder =>
                    policyBuilder.WaitAndRetryAsync(3, _ => TimeSpan.FromSeconds(3d)));
        }

        internal static void AddLocalizationServices(this IServiceCollection services,
            IReadOnlyDictionary<string, LocalizationModel> localizationModels)
        {
            services.AddSingleton(localizationModels);
        }

        internal static void AddReviewBot(this IServiceCollection services)
        {
            services.AddHostedService<ReviewBotWorkerService>();
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

        internal static void AddStatsBot(this IServiceCollection services)
        {
            services.AddSingleton<ChartClient>();
            services.AddHostedService<StatsBotWorkerService>();
            
            services.AddHttpClient<ChartClient>(chartClient =>
                {
                    chartClient.BaseAddress = new Uri("https://image-charts.com/");
                })
                .AddTransientHttpErrorPolicy(policyBuilder =>
                    policyBuilder.WaitAndRetryAsync(10, _ => TimeSpan.FromSeconds(10d)));
        }
    }
}