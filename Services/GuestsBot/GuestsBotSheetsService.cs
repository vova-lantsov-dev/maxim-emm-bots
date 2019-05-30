using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MaximEmmBots.Models.Json;
using MaximEmmBots.Models.Mongo;
using MaximEmmBots.Options;
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
        private readonly GoogleSheetsService _googleSheetsService;
        private readonly CultureService _cultureService;

        public GuestsBotSheetsService(ITelegramBotClient client,
            IOptions<DataOptions> dataOptions,
            Context context,
            ILogger<GoogleSheetsService> logger,
            GoogleSheetsService googleSheetsService, CultureService cultureService)
        {
            _client = client;
            _data = dataOptions.Value.Data;
            _context = context;
            _logger = logger;
            _googleSheetsService = googleSheetsService;
            _cultureService = cultureService;
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

                var today = _cultureService.NowFor(restaurant).Date;

                var questions = response.Values[0].Select(questionColumn => questionColumn.ToString()).ToList();
                var russianCulture = _cultureService.CultureFor(restaurant);

                foreach (var row in response.Values.Skip(1))
                {
                    if (row.Count <= 1)
                        continue;

                    if (!DateTime.TryParseExact(row[0].ToString(), "G", russianCulture,
                            DateTimeStyles.AllowWhiteSpaces, out var rowDate) || rowDate.Date != today)
                        continue;
                    
                    try
                    {
                        var filter = Builders<SentForm>.Filter.And(
                        Builders<SentForm>.Filter.Eq(f => f.Date, rowDate.Date),
                        Builders<SentForm>.Filter.ElemMatch(f => f.Items, it => it.SentTime == rowDate.TimeOfDay),
                        Builders<SentForm>.Filter.Eq(f => f.RestaurantId, restaurant.ChatId));
                    
                        if (await _context.SentForms.Find(filter).AnyAsync(stoppingToken))
                            continue;

                        await _context.SentForms.UpdateOneAsync(filter, Builders<SentForm>.Update
                                .Push(c => c.Items,
                                    new SentFormItem
                                    {
                                        SentTime = rowDate.TimeOfDay,
                                        EmployeeName = row[restaurant.GuestsBot.ColumnOfName - 1].ToString()
                                    })
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