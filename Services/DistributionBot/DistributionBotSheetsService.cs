using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MaximEmmBots.Models.Json.Restaurants;
using MaximEmmBots.Models.Mongo;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace MaximEmmBots.Services.DistributionBot
{
    internal sealed class DistributionBotSheetsService
    {
        private readonly ITelegramBotClient _client;
        private readonly ILogger<GoogleSheetsService> _logger;
        private readonly GoogleSheetsService _googleSheetsService;
        private readonly CultureService _cultureService;
        private readonly Context _context;
        private readonly IHostEnvironment _env;

        public DistributionBotSheetsService(ITelegramBotClient client,
            ILogger<GoogleSheetsService> logger,
            GoogleSheetsService googleSheetsService,
            CultureService cultureService,
            Context context,
            IHostEnvironment env)
        {
            _client = client;
            _logger = logger;
            _googleSheetsService = googleSheetsService;
            _cultureService = cultureService;
            _context = context;
            _env = env;
        }

        internal Task ExecuteManyAsync(ICollection<Restaurant> restaurants, CancellationToken stoppingToken,
            int userId, DateTime? requestedDate = null)
        {
            if (restaurants.Count == 0)
                return Task.CompletedTask;
            
            return Task.WhenAll(restaurants.Select(restaurant =>
                ExecuteAsync(restaurant, stoppingToken, requestedDate, userId)));
        }

        internal async Task ExecuteAsync(Restaurant restaurant, CancellationToken stoppingToken,
            DateTime? requestedDate = null, int userId = 0)
        {
            var forDate = requestedDate ?? _cultureService.NowFor(restaurant).AddDays(1d);
            var culture = _cultureService.CultureFor(restaurant);
            var monthName = culture.DateTimeFormat.GetMonthName(forDate.Month);
            var model = _cultureService.ModelFor(restaurant);
            
            var range = $"{monthName} {forDate:MM/yyyy}!$A$1:$YY";

            if (_env.IsDevelopment())
            {
                await _client.SendTextMessageAsync(restaurant.ChatId, $"Range is {range}\n\nRequested date is {forDate}",
                    cancellationToken: stoppingToken);
            }
            
            var response =
                await _googleSheetsService.GetValueRangeAsync(restaurant.DistributionBot.SpreadsheetId, range,
                    stoppingToken);

            if (response?.Values == null || response.Values.Count == 0)
            {
                if (userId > 0)
                    await _client.SendTextMessageAsync(userId, model.TimeBoardIsNotAvailableForThisMonth,
                        cancellationToken: stoppingToken);
                return;
            }

            var day = forDate.Day;
            var dateText = forDate.ToString("D", culture);
            var privates = new Dictionary<int, string>();
            var users = new List<(int userId, string name, string time)>();
            
            foreach (var row in response.Values)
            {
                if (row.Count < day + 3 ||
                    !int.TryParse(row[1].ToString().AsSpan(), out var rowUserId) &&
                    row[2].ToString().AsSpan().IndexOf('+') != 0 ||
                    privates.ContainsKey(rowUserId))
                    continue;

                var dayText = row[day + 2].ToString();
                if (string.IsNullOrWhiteSpace(dayText))
                    continue;

                {
                    var place = dayText[0];
                    if (char.IsLetter(place))
                    {
                        if (restaurant.PlaceId != place || dayText.Length == 1)
                            continue;

                        dayText = new string(dayText.AsSpan(1).TrimStart());
                    }
                }

                var name = row[0].ToString();

                users.Add((rowUserId, name, dayText));

                if (rowUserId == 0 || userId > 0 && rowUserId != userId)
                    continue;

                privates[rowUserId] = string.Format(model.YouWorkAt, name, dateText,
                    new string($"{dayText} {restaurant.PlaceInfo}".AsSpan().TrimEnd()));
            }
            
            if (users.Count == 0)
                return;
            
            foreach (var (privateUserId, privateText) in privates)
            {
                var privateTextBuilder = new StringBuilder();
                privateTextBuilder.Append(privateText);
                privateTextBuilder.Append("\n\n");
                privateTextBuilder.AppendFormat(model.WhoWorksWithYou, dateText);
                privateTextBuilder.Append('\n');
                privateTextBuilder.AppendJoin('\n', users.Where(u => u.userId != privateUserId).Select(
                    u => u.userId > 0
                        ? string.Format(model.TimeForUserWithTelegram, u.name, u.time, u.userId)
                        : string.Format(model.TimeForUserWithoutTelegram, u.name, u.time)));

                try
                {
                    /*await _client.SendTextMessageAsync(privateUserId, privateTextBuilder.ToString(),
                        ParseMode.Html, cancellationToken: stoppingToken);*/
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Unable to send a message to private chat");
                }

                try
                {

                    await _context.UserRestaurantPairs
                        .UpdateOneAsync(ur => ur.RestaurantId == restaurant.ChatId && ur.UserId == privateUserId,
                        Builders<UserRestaurantPair>.Update.SetOnInsert(ur => ur.Id, ObjectId.GenerateNewId()),
                        new UpdateOptions {IsUpsert = true}, stoppingToken);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Unable to save user id to UserRestaurantPairs");
                }
            }

            if (userId == 0)
            {
                var groupTextBuilder = new StringBuilder();
                groupTextBuilder.AppendFormat(model.WhoWorksAtDate, dateText, restaurant.PlaceInfo);
                groupTextBuilder.Append('\n');
                groupTextBuilder.AppendJoin('\n', users.Select(u => u.userId > 0
                    ? string.Format(model.TimeForUserWithTelegram, u.name, u.time, u.userId)
                    : string.Format(model.TimeForUserWithoutTelegram, u.name, u.time)));

                try
                {
                    await _client.SendTextMessageAsync(restaurant.ChatId, groupTextBuilder.ToString(),
                        ParseMode.Html, cancellationToken: stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unable to send a message to group");
                }
            }
        }
    }
}