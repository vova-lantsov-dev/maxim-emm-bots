using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MaximEmmBots.Models.Json;
using MaximEmmBots.Options;
using MaximEmmBots.Services.DistributionBot;
using Microsoft.Extensions.Hosting;
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
        private readonly Data _data;
        private readonly Context _context;
        private readonly ILogger<BotHandler> _logger;
        private readonly HttpClient _httpClient;
        private readonly CultureService _cultureService;
        private readonly DistributionBotSheetsService _distributionBotSheetsService;
        private readonly IHostApplicationLifetime _lifetime;

        public BotHandler(ILogger<BotHandler> logger,
            IOptions<DataOptions> dataOptions,
            Context context,
            HttpClient httpClient,
            CultureService cultureService,
            DistributionBotSheetsService distributionBotSheetsService,
            IHostApplicationLifetime lifetime)
        {
            _logger = logger;
            _data = dataOptions.Value.Data;
            _context = context;
            _httpClient = httpClient;
            _cultureService = cultureService;
            _distributionBotSheetsService = distributionBotSheetsService;
            _lifetime = lifetime;
        }

        public async Task HandleUpdate(ITelegramBotClient client, Update update, CancellationToken cancellationToken)
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

                    var model = _cultureService.ModelFor(restaurant);

                    var separated = q.Data.Split('~');
                    if (separated.Length == 0)
                        return;

                    switch (separated[0])
                    {
                        case "comments" when separated.Length == 2:
                        {
                            var review = await _context.Reviews.Find(r => r.Id == separated[1])
                                .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
                            if (review == default)
                            {
                                _logger.LogDebug("Review for this command was not found");
                                break;
                            }

                            await client.EditMessageTextAsync(restaurant.ChatId, q.Message.MessageId,
                                string.Concat(review.ToString(
                                        _cultureService.CultureFor(restaurant),
                                        _cultureService.ModelFor(restaurant),
                                        _data.ReviewBot.MaxValuesOfRating.TryGetValue(review.Resource,
                                            out var maxValueOfRating)
                                            ? maxValueOfRating
                                            : -1,
                                        _data.ReviewBot.PreferAvatarOverProfileLinkFor.Contains(review.Resource)),
                                    "\n\n", model.Comments, "\n\n", string.Join("\n\n", review.Comments)),
                                ParseMode.Html, replyMarkup: !review.IsReadOnly && review.ReplyLink != null
                                    ? new InlineKeyboardButton {Text = model.OpenReview, Url = review.ReplyLink}
                                    : null, cancellationToken: cancellationToken).ConfigureAwait(false);

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

                    await client.AnswerCallbackQueryAsync(update.CallbackQuery.Id,
                        cancellationToken: cancellationToken).ConfigureAwait(false);

                    break;
                }

                case UpdateType.Message
                    when update.Message.Text == "/restart_bot" &&
                         _data.WithRestartAccess?.Contains(update.Message.From.Id) == true:
                {
                    try
                    {
                        await client.DeleteMessageAsync(update.Message.Chat, update.Message.MessageId,
                            cancellationToken);
                    }
                    catch
                    {
                        // silent mode
                    }

                    _lifetime.StopApplication();
                    break;
                }

                case UpdateType.Message
                    when update.Message.Type == MessageType.Text:
                {
                    var m = update.Message;

                    var restaurant = _data.Restaurants.Find(r => r.ChatId == m.Chat.Id);

                    if (restaurant != default && m.ReplyToMessage != default)
                    {
                        if (!restaurant.AdminIds.Contains(m.From.Id))
                            break;

                        var model = _cultureService.ModelFor(restaurant);

                        var googleReviewMessage = await _context.GoogleReviewMessages
                            .Find(grm => grm.MessageId == m.ReplyToMessage.MessageId &&
                                         grm.ChatId == m.Chat.Id).FirstOrDefaultAsync(cancellationToken)
                            .ConfigureAwait(false);
                        if (googleReviewMessage == default)
                            return;

                        var review = await _context.Reviews.Find(r => r.Id == googleReviewMessage.ReviewId)
                            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
                        if (review == default)
                        {
                            await _context.GoogleReviewMessages.DeleteOneAsync(r =>
                                r.ReviewId == googleReviewMessage.ReviewId, cancellationToken).ConfigureAwait(false);
                            return;
                        }

                        var googleCredential = await _context.GoogleCredentials.Find(gc => gc.Name == "google")
                            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
                        if (googleCredential == default)
                        {
                            _logger.LogDebug("Google credential not found");
                            return;
                        }

                        await using var jsonStream = new MemoryStream();
                        await JsonSerializer.SerializeAsync(jsonStream, new {comment = update.Message.Text},
                            cancellationToken: cancellationToken).ConfigureAwait(false);

                        var httpRequest = new HttpRequestMessage(HttpMethod.Put, review.ReplyLink);
                        httpRequest.Headers.Authorization =
                            new AuthenticationHeaderValue("Bearer", googleCredential.AccessToken);
                        httpRequest.Content = new StreamContent(jsonStream);
                        httpRequest.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
                        await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);

                        await client.SendTextMessageAsync(m.Chat, model.ResponseToReviewSent,
                            replyToMessageId: m.MessageId, cancellationToken: cancellationToken).ConfigureAwait(false);
                    }
                    else if (restaurant != default &&
                             m.Text.StartsWith(_data.Bot.GroupReloadCommand, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!restaurant.AdminIds.Contains(m.From.Id))
                            break;

                        var separated = m.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (separated.Length >= 2 && DateTime.TryParseExact(separated[1], "d",
                            _cultureService.CultureFor(restaurant), DateTimeStyles.None, out var requestedDate))
                        {
                            await _distributionBotSheetsService.ExecuteAsync(restaurant, cancellationToken,
                                requestedDate).ConfigureAwait(false);
                        }
                        else
                        {
                            await _distributionBotSheetsService.ExecuteAsync(restaurant, cancellationToken)
                                .ConfigureAwait(false);
                        }
                    }
                    else if (m.Chat.Type == ChatType.Private && m.Text.StartsWith(_data.Bot.PrivateReloadCommand,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        var separated = m.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                        var restaurantIds = await _context.UserRestaurantPairs.Find(ur => ur.UserId == m.From.Id)
                            .Project(ur => ur.RestaurantId).ToListAsync(cancellationToken).ConfigureAwait(false);
                        var restaurants = _data.Restaurants.FindAll(r => restaurantIds.Contains(r.ChatId));

                        if (separated.Length < 2)
                        {
                            await _distributionBotSheetsService.ExecuteManyAsync(restaurants, cancellationToken,
                                m.From.Id).ConfigureAwait(false);
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
                            requestedDate).ConfigureAwait(false);
                    }

                    break;
                }

                case UpdateType.Message when update.Message.Type == MessageType.ChatMembersAdded:
                {
                    var m = update.Message;
                    var restaurant = _data.Restaurants.Find(r => r.ChatId == m.Chat.Id);
                    if (restaurant == default)
                        break;

                    var model = _cultureService.ModelFor(restaurant);
                    var culture = _cultureService.CultureFor(restaurant);

                    foreach (var member in m.NewChatMembers)
                    {
                        try
                        {
                            var text = string.Format(culture, model.NewMemberInGroup, member.FirstName, member.Id,
                                _data.Bot.Username);
                            await client.SendTextMessageAsync(m.Chat, text, ParseMode.Html,
                                cancellationToken: cancellationToken).ConfigureAwait(false);
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, "Cannot send a message to group with id {0}", m.Chat.Id);
                        }

                        var adminText = string.Format(culture, model.NewMemberForAdmin, member.Id, member.FirstName,
                            member.LastName, restaurant.Name);
                        foreach (var adminId in restaurant.AdminIds)
                        {
                            try
                            {
                                await client.SendTextMessageAsync(adminId, adminText, ParseMode.Html,
                                    cancellationToken: cancellationToken).ConfigureAwait(false);
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

        public Task HandleError(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Error occurred while handling an incoming update.");
            return Task.CompletedTask;
        }

        public UpdateType[] AllowedUpdates => new[] { UpdateType.Message, UpdateType.CallbackQuery };
    }
}