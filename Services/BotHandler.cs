using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MaximEmmBots.Models.Json;
using MaximEmmBots.Options;
using MaximEmmBots.Services.DistributionBot;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

// ReSharper disable SwitchStatementMissingSomeCases

namespace MaximEmmBots.Services
{
    internal sealed class BotHandler : IUpdateHandler
    {
        private readonly ITelegramBotClient _client;
        private readonly Data _data;
        private readonly Context _context;
        private readonly ILogger<BotHandler> _logger;
        private readonly HttpClient _httpClient;
        private readonly IReadOnlyDictionary<string, LocalizationModel> _models;
        private readonly CultureService _cultureService;
        private readonly DistributionBotSheetsService _distributionBotSheetsService;
        
        public BotHandler(ILogger<BotHandler> logger,
            ITelegramBotClient client,
            IOptions<DataOptions> dataOptions,
            Context context,
            HttpClient httpClient,
            IReadOnlyDictionary<string, LocalizationModel> models,
            CultureService cultureService,
            DistributionBotSheetsService distributionBotSheetsService)
        {
            _logger = logger;
            _client = client;
            _data = dataOptions.Value.Data;
            _context = context;
            _httpClient = httpClient;
            _models = models;
            _cultureService = cultureService;
            _distributionBotSheetsService = distributionBotSheetsService;
        }
        
        public async Task HandleUpdate(Update update, CancellationToken cancellationToken)
        {
            switch (update.Type)
            {
                case UpdateType.CallbackQuery when update.CallbackQuery.Message != null:
                {
                    _logger.LogInformation("Callback query received from user {0} in chat {1}, data is {2}",
                        update.CallbackQuery.From.Id, update.CallbackQuery.Message.Chat.Id,
                        update.CallbackQuery.Data);
                    
                    var q = update.CallbackQuery;
                    var restaurant = _data.Restaurants.Find(r => r.ChatId == q.Message.Chat.Id);
                    if (restaurant == default)
                    {
                        _logger.LogDebug("Restaurant is null, breaking this update");
                        break;
                    }

                    var model = _models[restaurant.Culture.Name];
                    
                    var separated = q.Data.Split('~');
                    if (separated.Length == 0)
                        return;

                    switch (separated[0])
                    {
                        case "comments" when separated.Length == 2:
                        {
                            var review = await _context.Reviews.Find(r => r.Id == separated[1])
                                .SingleOrDefaultAsync(cancellationToken);
                            if (review == default)
                            {
                                _logger.LogDebug("Review for this command was not found");
                                break;
                            }

                            await _client.EditMessageTextAsync(restaurant.ChatId, q.Message.MessageId,
                                string.Concat(review, "\n\n", model.Comments, "\n\n",
                                    string.Join("\n\n", review.Comments)),
                                ParseMode.Markdown, replyMarkup: !review.IsReadOnly && review.ReplyLink != null
                                    ? new InlineKeyboardButton {Text = model.OpenReview, Url = review.ReplyLink}
                                    : null, cancellationToken: cancellationToken);

                            break;
                        }

                        default:
                        {
                            _logger.LogWarning("Received bad request. separated[1] == \"{0}\". \n\nMaybe, " +
                                               "something works wrong. Please, contact the developer.\n\nChat id is {1}",
                                separated[1], restaurant.ChatId);
                            break;
                        }
                    }

                    await _client.AnswerCallbackQueryAsync(update.CallbackQuery.Id,
                        cancellationToken: cancellationToken);
                    
                    break;
                }

                case UpdateType.Message
                    when update.Message.Type == MessageType.Text &&
                         update.Message.ReplyToMessage != null:
                {
                    var m = update.Message;
                    
                    var restaurant = _data.Restaurants.Find(r => r.ChatId == m.Chat.Id);

                    if (restaurant != default && m.ReplyToMessage != default)
                    {
                        if (!restaurant.AdminIds.Contains(m.From.Id))
                            break;

                        var model = _models[restaurant.Culture.Name];

                        var googleReviewMessage = await _context.GoogleReviewMessages
                            .Find(grm => grm.MessageId == m.ReplyToMessage.MessageId &&
                                         grm.ChatId == m.Chat.Id).FirstOrDefaultAsync(cancellationToken);
                        if (googleReviewMessage == default)
                            return;

                        var review = await _context.Reviews.Find(r => r.Id == googleReviewMessage.ReviewId)
                            .FirstOrDefaultAsync(cancellationToken);
                        if (review == default)
                        {
                            await _context.GoogleReviewMessages.DeleteOneAsync(r =>
                                r.ReviewId == googleReviewMessage.ReviewId, cancellationToken);
                            return;
                        }

                        var googleCredential = await _context.GoogleCredentials.Find(gc => gc.Name == "google")
                            .FirstOrDefaultAsync(cancellationToken);
                        if (googleCredential == default)
                        {
                            _logger.LogDebug("Google credential not found");
                            return;
                        }

                        var jsonContent = JsonSerializer.ToString(new {comment = update.Message.Text});

                        var httpRequest = new HttpRequestMessage(HttpMethod.Put, review.ReplyLink);
                        httpRequest.Headers.Authorization =
                            new AuthenticationHeaderValue("Bearer", googleCredential.AccessToken);
                        httpRequest.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                        await _httpClient.SendAsync(httpRequest, cancellationToken);

                        await _client.SendTextMessageAsync(m.Chat, model.ResponseToReviewSent,
                            replyToMessageId: m.MessageId, cancellationToken: cancellationToken);
                    }
                    else if (restaurant != default && m.Text.StartsWith(_data.Bot.GroupReloadCommand, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!restaurant.AdminIds.Contains(m.From.Id))
                            break;

                        var separated = m.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (separated.Length >= 2 && DateTime.TryParseExact(separated[1], "d",
                                _cultureService.CultureFor(restaurant), DateTimeStyles.None, out var requestedDate))
                        {
                            await _distributionBotSheetsService.ExecuteAsync(restaurant, cancellationToken,
                                requestedDate);
                        }
                        else
                        {
                            await _distributionBotSheetsService.ExecuteAsync(restaurant, cancellationToken);
                        }
                    }
                    else if (m.Chat.Type == ChatType.Private && m.Text.StartsWith(_data.Bot.PrivateReloadCommand, StringComparison.OrdinalIgnoreCase))
                    {
                        var separated = m.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        
                        var restaurantIds = await _context.UserRestaurantPairs.Find(ur => ur.UserId == m.From.Id)
                            .Project(ur => ur.RestaurantId).ToListAsync(cancellationToken);
                        var restaurants = _data.Restaurants.FindAll(r => restaurantIds.Contains(r.ChatId));
                        
                        if (separated.Length < 2)
                        {
                            await _distributionBotSheetsService.ExecuteManyAsync(restaurants, cancellationToken,
                                m.From.Id);
                            return;
                        }
                        
                        DateTime requestedDate = default;
                        foreach (var tempRestaurant in restaurants)
                            if (DateTime.TryParseExact(separated[1], "d", _cultureService.CultureFor(tempRestaurant),
                                DateTimeStyles.None, out requestedDate))
                                break;
                        if (requestedDate == default)
                            return;

                        await _distributionBotSheetsService.ExecuteManyAsync(restaurants, cancellationToken, m.From.Id,
                            requestedDate);
                    }

                    break;
                }

                case UpdateType.Message when update.Message.Type == MessageType.ChatMembersAdded:
                {
                    var m = update.Message;
                    var restaurant = _data.Restaurants.Find(r => r.ChatId == m.Chat.Id);
                    if (restaurant == default)
                        break;

                    var model = _models[restaurant.Culture.Name];
                    
                    foreach (var member in m.NewChatMembers)
                    {
                        try
                        {
                            var text = string.Format(model.NewMemberInGroup, member.FirstName, member.Id,
                                _data.Bot.Username);
                            await _client.SendTextMessageAsync(m.Chat, text, ParseMode.Html,
                                cancellationToken: cancellationToken);
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, "Cannot send a message to group with id {0}", m.Chat.Id);
                        }

                        var adminText = string.Format(model.NewMemberForAdmin, member.Id, member.FirstName,
                            member.LastName, restaurant.Name);
                        foreach (var adminId in restaurant.AdminIds)
                        {
                            try
                            {
                                await _client.SendTextMessageAsync(adminId, adminText, ParseMode.Html,
                                    cancellationToken: cancellationToken);
                            }
                            catch (Exception e)
                            {
                                _logger.LogError(e, "Cannot send a message to private chat with id {0}", adminId);
                            }
                        }
                    }
                    break;
                }
            }
        }

        public Task HandleError(Exception exception, CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Error occurred while handling an incoming update.");
            return Task.CompletedTask;
        }

        public UpdateType[] AllowedUpdates => new[] {UpdateType.Message, UpdateType.CallbackQuery};
    }
}