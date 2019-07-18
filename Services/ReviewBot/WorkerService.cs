using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MaximEmmBots.Models.Json;
using MaximEmmBots.Models.Mongo;
using MaximEmmBots.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

// ReSharper disable MethodSupportsCancellation

namespace MaximEmmBots.Services.ReviewBot
{
    internal sealed class WorkerService : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly Data _data;
        private readonly Context _context;
        private readonly ITelegramBotClient _client;
        private readonly CultureService _cultureService;
        private readonly IHostEnvironment _env;
        
        public WorkerService(IOptions<DataOptions> options,
            ILoggerFactory loggerFactory,
            Context context,
            ITelegramBotClient client,
            CultureService cultureService,
            IHostEnvironment env)
        {
            _data = options.Value.Data;
            _logger = loggerFactory.CreateLogger("ReviewBotWorkerService");
            _context = context;
            _client = client;
            _cultureService = cultureService;
            _env = env;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting ReviewBotWorkerService...");
            
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.WhenAll(GetWorkerTask(stoppingToken),
                    Task.Delay(TimeSpan.FromMinutes(60d), stoppingToken));
            }
        }

        private async Task GetWorkerTask(CancellationToken cancellationToken)
        {
            try
            {
                await Task.WhenAny(GetScriptRunnerTask(cancellationToken),
                    Task.Delay(TimeSpan.FromMinutes(10d), cancellationToken));
                await GetNotifierTask(cancellationToken);
            }
            catch (Exception e) when (!(e is OperationCanceledException))
            {
                _logger.LogError(e, "Error occurred while running worker task");
            }
        }

        private Task GetScriptRunnerTask(CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                foreach (var restaurant in _data.Restaurants.Where(r => r.Urls != null))
                {
                    foreach (var (resource, link) in restaurant.Urls)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        
                        var processInfo = new ProcessStartInfo
                        {
                            WorkingDirectory = _data.ReviewBot.Script.WorkingDirectory,
                            Arguments =
                                string.Format(_data.ReviewBot.Script.Arguments, resource, link, restaurant.Name),
                            FileName = _data.ReviewBot.Script.FileName
                        };
                        var process = Process.Start(processInfo);
                        process?.WaitForExit();
                    }
                }
            }, cancellationToken);
        }
        
        private async Task GetNotifierTask(CancellationToken cancellationToken)
        {
            var notSentReviews = await _context.Reviews
                .Find(r => r.NeedToShow)
                .SortByDescending(r => r.Id)
                .ToListAsync(cancellationToken);

            if (notSentReviews.Count == 0)
            {
                if (_env.IsDevelopment())
                    _logger.LogInformation("Count of notSentReviews is 0.");
                
                return;
            }
            
            cancellationToken.ThrowIfCancellationRequested();
            
            foreach (var restaurantGroup in notSentReviews.GroupBy(r => r.RestaurantName))
            {
                var restaurant = _data.Restaurants.Find(r => r.Name == restaurantGroup.Key);
                if (restaurant == default)
                {
                    _logger.LogInformation("Restaurant named '{0}' was not found.", restaurantGroup.Key);
                    return;
                }

                foreach (var notSentReviewGroup in restaurantGroup.GroupBy(r => r.Resource))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    if (_env.IsDevelopment())
                    {
                        await _client.SendTextMessageAsync(restaurant.ChatId,
                            $"Resource: {notSentReviewGroup.Key}\nCount of reviews: {notSentReviewGroup.Count()}");
                    }
                    
                    foreach (var notSentReview in notSentReviewGroup.Take(5))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var model = _cultureService.ModelFor(restaurant);

                        var buttons = new List<InlineKeyboardButton[]>();
                        if ((notSentReview.Comments?.Count ?? 0) > 0)
                            buttons.Add(new[]
                            {
                                new InlineKeyboardButton
                                {
                                    Text = model.ViewFeedback,
                                    CallbackData = $"comments~{notSentReview.Id}"
                                }
                            });
                        if (!notSentReview.IsReadOnly && notSentReview.ReplyLink != null &&
                            notSentReview.Resource != "google")
                            buttons.Add(new[]
                            {
                                new InlineKeyboardButton {Text = model.OpenReview, Url = notSentReview.ReplyLink}
                            });

                        var chatId = restaurant.ChatId;
                        var sentMessage = await _client.SendTextMessageAsync(chatId, notSentReview.ToString(model,
                                _data.ReviewBot.MaxValuesOfRating.TryGetValue(notSentReview.Resource,
                                    out var maxValueOfRating)
                                    ? maxValueOfRating
                                    : -1,
                                _data.ReviewBot.PreferAvatarOverProfileLinkFor.Contains(notSentReview.Resource)),
                            ParseMode.Html, cancellationToken: cancellationToken, replyMarkup: buttons.Count > 0
                                ? new InlineKeyboardMarkup(buttons)
                                : null);

                        if (notSentReview.Resource == "google")
                            await _context.GoogleReviewMessages.UpdateOneAsync(grm => grm.ReviewId == notSentReview.Id,
                                Builders<GoogleReviewMessage>.Update.Set(grm => grm.MessageId, sentMessage.MessageId),
                                new UpdateOptions {IsUpsert = true});

                        await _context.Reviews.UpdateOneAsync(r => r.Id == notSentReview.Id,
                            Builders<Review>.Update.Set(r => r.NeedToShow, false));
                    }

                    await _context.Reviews.UpdateManyAsync(r => r.Resource == notSentReviewGroup.Key && r.NeedToShow,
                        Builders<Review>.Update.Set(r => r.NeedToShow, false));
                }
            }
        }
    }
}