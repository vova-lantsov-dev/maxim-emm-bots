using System;
using System.Globalization;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using MaximEmmBots.Models.Json.Restaurants;
using MaximEmmBots.Models.Mongo;
using MaximEmmBots.Services.Scheduling;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace MaximEmmBots.Services.GuestsBot
{
    internal sealed class GuestsBotScheduler : IScheduler
    {
        private readonly ITelegramBotClient _client;
        private readonly Context _context;
        private readonly ILogger<GoogleSheetsService> _logger;
        private readonly GoogleSheetsService _googleSheetsService;
        private readonly CultureService _cultureService;
        private readonly IHostEnvironment _env;

        public GuestsBotScheduler(ITelegramBotClient client,
            Context context,
            ILogger<GoogleSheetsService> logger,
            GoogleSheetsService googleSheetsService,
            CultureService cultureService,
            IHostEnvironment env)
        {
            _env = env;
            _client = client;
            _context = context;
            _logger = logger;
            _googleSheetsService = googleSheetsService;
            _cultureService = cultureService;
        }
        
        public string SchedulerName => nameof(GuestsBotScheduler);

        public SchedulingMode SchedulingMode => SchedulingMode.Static;
        
        public Func<Restaurant, TimeSpan> SchedulingTime => r => r.GuestsBot != null ? TimeSpan.FromMinutes(5d) : Timeout.InfiniteTimeSpan;

        public async Task OnElapseAsync(Restaurant restaurant, CancellationToken cancellationToken)
        {
            var spreadsheetId = $"{restaurant.GuestsBot.TableName}!$A$1:$YY";
            var response = await _googleSheetsService.GetValueRangeAsync(restaurant.GuestsBot.SpreadsheetId,
                spreadsheetId, cancellationToken);
            
            if (response?.Values == null || response.Values.Count == 0)
            {
                if (_env.IsDevelopment())
                    await _client.SendTextMessageAsync(restaurant.ChatId,
                        $"Response for {SchedulerName} is null or empty.",
                        cancellationToken: cancellationToken);
                return;
            }

            var today = _cultureService.NowFor(restaurant).Date;
            var culture = _cultureService.CultureFor(restaurant);

            var questions = response.Values[0].Select(questionColumn => questionColumn.ToString()).ToList();

            foreach (var row in response.Values.Skip(1))
            {
                if (row.Count <= 1)
                    continue;

                if (!DateTime.TryParseExact(row[0].ToString(), "G", culture,
                        DateTimeStyles.AllowWhiteSpaces, out var rowDate) || rowDate.Date != today)
                    continue;

                var filter = Builders<SentForm>.Filter.And(
                    Builders<SentForm>.Filter.Eq(f => f.Date, rowDate.Date),
                    Builders<SentForm>.Filter.ElemMatch(f => f.Items, it => it.SentTime == rowDate.TimeOfDay),
                    Builders<SentForm>.Filter.Eq(f => f.RestaurantId, restaurant.ChatId));

                try
                {
                    if (await _context.SentForms.Find(filter).AnyAsync(cancellationToken))
                        continue;

                    await _context.SentForms.UpdateOneAsync(filter, Builders<SentForm>.Update
                            .Push(c => c.Items,
                                new SentFormItem
                                {
                                    SentTime = rowDate.TimeOfDay,
                                    EmployeeName = row[restaurant.GuestsBot.ColumnOfName - 1].ToString()
                                })
                            .SetOnInsert(c => c.Id, ObjectId.GenerateNewId()),
                        new UpdateOptions {IsUpsert = true}, cancellationToken);

                    await _client.SendTextMessageAsync(restaurant.ChatId,
                        string.Join('\n', questions.Take(row.Count).Select((q, i) =>
                        {
                            var answer = row[i];
                            return $"<b>{HtmlEncoder.Default.Encode(q)}</b>: {HtmlEncoder.Default.Encode(answer.ToString())}";
                        }).Prepend($"<b>{restaurant.Name}</b>")), ParseMode.Html, cancellationToken: cancellationToken);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error occurred while executing guests bot for restaurant id {0}",
                        restaurant.ChatId);
                }
            }
        }
    }
}