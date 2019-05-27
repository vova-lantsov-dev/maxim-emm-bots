using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using MaximEmmBots.Models.Json;
using MaximEmmBots.Models.Mongo;
using MaximEmmBots.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace MaximEmmBots.Services
{
    internal sealed class GoogleSheetsService
    {
        private readonly SheetsService _sheetsService;
        private readonly ITelegramBotClient _client;
        private readonly Data _data;
        private readonly Context _context;
        private readonly ILogger<GoogleSheetsService> _logger;
        private readonly IReadOnlyDictionary<string, LocalizationModel> _localizationModels;
        private readonly IReadOnlyDictionary<string, TimeZoneInfo> _timeZones;
        private readonly IReadOnlyDictionary<string, CultureInfo> _cultures;

        private static readonly CultureInfo RussianCulture = new CultureInfo("ru-RU");
        
        public GoogleSheetsService(IOptions<DataOptions> options, ITelegramBotClient client,
            SheetsService sheetsService, ILogger<GoogleSheetsService> logger,
            IReadOnlyDictionary<string, LocalizationModel> localizationModels,
            IReadOnlyDictionary<string, TimeZoneInfo> timeZones, IReadOnlyDictionary<string, CultureInfo> cultures,
            Context context)
        {
            _client = client;
            _sheetsService = sheetsService;
            _logger = logger;
            _localizationModels = localizationModels;
            _timeZones = timeZones;
            _cultures = cultures;
            _context = context;
            _data = options.Value.Data;
        }
        
        private async Task<ValueRange> GetValueRangeAsync(string sId, string range, CancellationToken stoppingToken)
        {
            _logger.LogDebug("SpreadsheetId is {0}", sId);
            _logger.LogDebug("Range is {0}", range);
            
            var request = _sheetsService.Spreadsheets.Values.Get(sId, range);

            try
            {
                return await request.ExecuteAsync(stoppingToken);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Error occurred while executing spreadsheet request.");
                return null;
            }
        }

        internal async Task ExecuteForGuestsBotAsync(CancellationToken stoppingToken)
        {
            foreach (var restaurant in _data.Restaurants)
            {
                var spreadsheetId = $"{restaurant.GuestsBot.TableName}!$A$1:$YY";
                var response = await GetValueRangeAsync(restaurant.GuestsBot.SpreadsheetId,
                    spreadsheetId, stoppingToken);
                if (response?.Values == null || response.Values.Count == 0)
                {
                    _logger.LogError("Spreadsheet value range is null or empty. Spreadsheet id: {0}", spreadsheetId);
                    return;
                }

                var today = TimeZoneInfo.ConvertTime(DateTime.UtcNow, _timeZones[restaurant.Culture.TimeZone]).AddDays(-1d).Date;

                var questions = response.Values[0].Select(questionColumn => questionColumn.ToString()).ToList();
                
                foreach (var row in response.Values.Skip(1))
                {
                    if (row.Count <= 1)
                        continue;

                    if (!DateTime.TryParseExact(row[0].ToString(), "G", RussianCulture,
                            DateTimeStyles.AllowWhiteSpaces, out var rowDate) || rowDate.Date != today)
                        continue;
                    
                    var searchDate = rowDate.ToString("d", RussianCulture);
                    var searchTime = rowDate.ToString("T", RussianCulture);
                    
                    var filter = Builders<SentCounter>.Filter.And(
                        Builders<SentCounter>.Filter.Eq(c => c.Date, searchDate),
                                    new BsonDocument("SentTimes", new BsonDocument("$elemMatch", new BsonDocument("$eq", searchTime))),
                                    Builders<SentCounter>.Filter.Eq(c => c.RestaurantId, restaurant.ChatId));
                    
                    try
                    {
                        if (await _context.SentCounters.Find(filter).AnyAsync(stoppingToken))
                            continue;
                    
                        await _context.SentCounters.UpdateOneAsync(filter, Builders<SentCounter>.Update
                                .Push(c => c.SentTimes, searchTime)
                                .SetOnInsert(c => c.Id, ObjectId.GenerateNewId()),
                            new UpdateOptions {IsUpsert = true}, stoppingToken);

                        await _client.SendTextMessageAsync(restaurant.ChatId,
                            string.Join('\n', questions.Take(row.Count).Select((q, i) =>
                            {
                                var answer = row[i];
                                return $"<b>{q}</b>: {answer}";
                            }).Prepend($"<b>{restaurant.Name}</b>")), ParseMode.Html, cancellationToken: stoppingToken);
                    }
                    catch (Exception e)
                    {
                        // ignored
                    }
                }
            }
        }
        
        internal async Task ExecuteForDistributionBotAsync(string timeZoneName, string cultureName,
            CancellationToken stoppingToken, int userId = 0)
        {
            var tomorrow = TimeZoneInfo.ConvertTime(DateTime.UtcNow, _timeZones[timeZoneName]).AddDays(1d);
            var culture = _cultures[cultureName];
            var monthName = culture.DateTimeFormat.GetMonthName(tomorrow.Month);
            var model = _localizationModels[cultureName];
            
            var range = $"{monthName} {tomorrow:MM/yyyy}!$A$1:$YY";
            var response = await GetValueRangeAsync(_data.DistributionBot.SpreadsheetId, range, stoppingToken);
            if (response?.Values == null || response.Values.Count == 0)
            {
                if (userId > 0)
                    await _client.SendTextMessageAsync(userId, "Расписание на этот месяц недоступно.",
                        cancellationToken: stoppingToken);
                return;
            }

            var day = tomorrow.Day;
            var dateText = tomorrow.ToString("dd MMMM yyyy", culture);
            var privates = new Dictionary<int, (string text, long chatId)>();
            var groups = new Dictionary<long, List<(int userId, string name, string time)>>();
            
            foreach (var row in response.Values)
            {
                if (row.Count < 3)
                    continue;
                var row2 = row[2].ToString();
                if (row.Count < day + 3 || !int.TryParse(row[1].ToString(), out var rowUserId) &&
                    (row2.Length == 0 || row2.IndexOfAny("@+78".ToCharArray()) != 0) || privates.ContainsKey(rowUserId))
                    continue;

                var dayText = row[day + 2].ToString();
                if (string.IsNullOrWhiteSpace(dayText))
                    continue;

                var place = char.ToLower(dayText[0], culture);
                var restaurant = _data.Restaurants.Find(r => r.PlaceId == place.ToString());
                if (restaurant == default)
                    continue;
                var name = row[0].ToString();

                var infoText = new StringBuilder();
                var i = 1;
                for (; i < dayText.Length; i++)
                {
                    if (infoText.Length == 2)
                        break;
                    
                    var character = dayText[i];
                    if (!char.IsDigit(character))
                        break;

                    infoText.Append(character);
                }
                
                infoText.Append(":00");
                infoText.Append(dayText.Length > i ? " " + dayText.Substring(i).TrimStart() : string.Empty);

                var groupItem = (rowUserId, name, infoText.ToString());
                if (!groups.ContainsKey(restaurant.ChatId))
                    groups[restaurant.ChatId] = new List<(int, string, string)> {groupItem};
                else groups[restaurant.ChatId].Add(groupItem);

                if (rowUserId == 0 || userId > 0 && rowUserId != userId)
                    continue;

                infoText.AppendFormat(" {0}", restaurant.PlaceInfo);
                
                privates.Add(rowUserId, ($"Дорогой(ая/ое) {name}, *{dateText}* ты работаешь с {infoText}!", restaurant.ChatId));
            }
            
            if (privates.Count == 0)
                return;
            
            foreach (var (privateUserId, (privateText, chatId)) in privates)
            {
                var privateTextBuilder = new StringBuilder();
                privateTextBuilder.Append(privateText);
                privateTextBuilder.Append("\n\n");
                privateTextBuilder.AppendFormat("Также {0} с тобой работают:\n", dateText);
                privateTextBuilder.AppendJoin('\n', groups[chatId].Where(u => u.userId != privateUserId).Select(
                    u => u.userId > 0
                        ? $"[{u.name}](tg://user?id={u.userId}) с {u.time}"
                        : $"*{u.name}* с {u.time}"));

                try
                {
                    await _client.SendTextMessageAsync(privateUserId, privateTextBuilder.ToString(),
                        ParseMode.Markdown, cancellationToken: stoppingToken);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Unable to send a message to private chat with id equals to {0}", privateUserId);
                }
            }

            if (userId == 0)
                foreach (var (chatId, users) in groups)
                {
                    var restaurant = _data.Restaurants.Find(r => r.ChatId == chatId);
                    if (restaurant == default)
                        continue;
                    
                    var groupTextBuilder = new StringBuilder();
                    groupTextBuilder.AppendFormat("*{0}* у нас {1} работают:\n", dateText, restaurant.PlaceInfo);
                    groupTextBuilder.AppendJoin('\n', users.Select(u => u.userId > 0
                        ? $"[{u.name}](tg://user?id={u.userId}) с {u.time}"
                        : $"*{u.name}* с {u.time}"));

                    try
                    {
                        await _client.SendTextMessageAsync(chatId, groupTextBuilder.ToString(),
                            ParseMode.Markdown, cancellationToken: stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unable to send a message to group with id equals to {0}", chatId);
                    }
                }
        }
    }
}