using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MaximEmmBots.Models.Json;
using MaximEmmBots.Models.Mongo;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace MaximEmmBots.Services.GuestsBot
{
    internal sealed class GuestsBotSheetsService
    {
        private readonly ITelegramBotClient _client;
        private readonly Data _data;
        private readonly Context _context;
        private readonly ILogger<GoogleSheetsService> _logger;
        private readonly IReadOnlyDictionary<string, TimeZoneInfo> _timeZones;
        private readonly IReadOnlyDictionary<string, CultureInfo> _cultures;
        private readonly GoogleSheetsService _googleSheetsService;

        public GuestsBotSheetsService(ITelegramBotClient client,
            IOptions<Data> dataOptions,
            Context context,
            ILogger<GoogleSheetsService> logger,
            IReadOnlyDictionary<string, TimeZoneInfo> timeZones,
            IReadOnlyDictionary<string, CultureInfo> cultures,
            GoogleSheetsService googleSheetsService)
        {
            _client = client;
            _data = dataOptions.Value;
            _context = context;
            _logger = logger;
            _timeZones = timeZones;
            _cultures = cultures;
            _googleSheetsService = googleSheetsService;
        }

        internal async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            foreach (var restaurant in _data.Restaurants)
            {
                var spreadsheetId = $"{restaurant.GuestsBot.TableName}!$A$1:$YY";
                var response = await _googleSheetsService.GetValueRangeAsync(restaurant.GuestsBot.SpreadsheetId,
                    spreadsheetId, stoppingToken);
                if (response?.Values == null || response.Values.Count == 0)
                {
                    _logger.LogError("Spreadsheet value range is null or empty. Spreadsheet id: {0}", spreadsheetId);
                    return;
                }

                var today = TimeZoneInfo.ConvertTime(DateTime.UtcNow, _timeZones[restaurant.Culture.TimeZone]).AddDays(-1d).Date;

                var questions = response.Values[0].Select(questionColumn => questionColumn.ToString()).ToList();
                var russianCulture = _cultures["ru-RU"];
                
                foreach (var row in response.Values.Skip(1))
                {
                    if (row.Count <= 1)
                        continue;

                    if (!DateTime.TryParseExact(row[0].ToString(), "G", russianCulture,
                            DateTimeStyles.AllowWhiteSpaces, out var rowDate) || rowDate.Date != today)
                        continue;
                    
                    var searchDate = rowDate.ToString("d", russianCulture);
                    var searchTime = rowDate.ToString("T", russianCulture);
                    
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
    }
}