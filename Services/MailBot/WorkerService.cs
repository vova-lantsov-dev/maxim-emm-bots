using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using MaximEmmBots.Models.Json;
using MaximEmmBots.Models.Mongo;
using MaximEmmBots.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using Telegram.Bot;

namespace MaximEmmBots.Services.MailBot
{
    internal sealed class WorkerService : BackgroundService
    {
        private readonly GoogleSheetsService _sheetsService;
        private readonly CultureService _cultureService;
        private readonly Context _context;
        private readonly Data _data;
        private readonly ITelegramBotClient _client;
        private readonly IMailClient _mailClient;

        public WorkerService(GoogleSheetsService sheetsService,
            CultureService cultureService,
            Context context,
            IOptions<DataOptions> options,
            ITelegramBotClient client,
            IMailClient mailClient)
        {
            _sheetsService = sheetsService;
            _cultureService = cultureService;
            _context = context;
            _client = client;
            _mailClient = mailClient;
            _data = options.Value.Data;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var valueRange = await _sheetsService.GetValueRangeAsync(_data.MailBot.SpreadsheetId,
                $"{_data.MailBot.TableName}!$A$1:$YY", cancellationToken).ConfigureAwait(false);
            if (valueRange == null)
                return;

            await foreach (var sentChecklist in YieldChecklistWatchdogEntryAsync(valueRange.Values,
                cancellationToken))
            {
                sentChecklist.Id = ObjectId.GenerateNewId();
                await _context.SentChecklists.InsertOneAsync(sentChecklist, null, cancellationToken).ConfigureAwait(false);
            }
        }

        private async IAsyncEnumerable<SentChecklist> YieldChecklistWatchdogEntryAsync(
            IList<IList<object>> rows, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var entries = new ChecklistWatchdogEntry[rows.Count - 1];
            for (var i = 0; i < entries.Length; i++)
            {
                var row = rows[i + 1];
                if (row.Count < 11)
                    continue;
                
                var entry = new ChecklistWatchdogEntry
                {
                    ChecklistName = row[1].ToString(),
                    ChecklistMessage = row[2].ToString(),
                    Type = ChecklistWatchdogEntryType.Daily,
                    NotifyFoundMessage = row[8].ToString(),
                    NotifyNotFoundMessage = row[10].ToString()
                };

                TimeSpan.TryParseExact(row[3].ToString(), @"hh\:mm", null, out entry.SendAt);

                {
                    var sendAtDaysOfWeek = row[4].ToString();
                    if (!string.IsNullOrEmpty(sendAtDaysOfWeek))
                    {
                        var separatedDaysOfWeek = sendAtDaysOfWeek.Split(',');
                        entry.PublishDaysOfWeek = new int[separatedDaysOfWeek.Length];
                        
                        for (var j = 0; j < separatedDaysOfWeek.Length; j++)
                            int.TryParse(separatedDaysOfWeek[j].AsSpan(), out entry.PublishDaysOfWeek[j]);
                        
                        entry.Type = ChecklistWatchdogEntryType.Weekly;
                    }
                }

                if (entry.Type != ChecklistWatchdogEntryType.Weekly)
                {
                    var sendAtDays = row[5].ToString();
                    if (!string.IsNullOrEmpty(sendAtDays))
                    {
                        var separatedDays = sendAtDays.Split(',');
                        entry.PublishDays = new Range[separatedDays.Length];
                        
                        for (var j = 0; j < separatedDays.Length; j++)
                        {
                            var day = separatedDays[j];
                            if (day.Contains('-', StringComparison.Ordinal))
                            {
                                var dayRange = day.Split('-', 2);
                                int.TryParse(dayRange[0], out var start);
                                int.TryParse(dayRange[1], out var end);
                                entry.PublishDays[j] = start..end;
                            }
                            else
                            {
                                int.TryParse(day, out var index);
                                entry.PublishDays[j] = index..index;
                            }
                        }

                        entry.Type = ChecklistWatchdogEntryType.Monthly;
                    }
                }

                long.TryParse(row[6].ToString().AsSpan(), out entry.PublishChatId);
                long.TryParse(row[7].ToString().AsSpan(), out entry.NotifyFoundChatId);
                long.TryParse(row[9].ToString().AsSpan(), out entry.NotifyNotFoundChatId);

                entries[i] = entry;
            }

            using var sentChecklists = new BlockingCollection<SentChecklist>();
            var tasks = new Task[entries.Length];
            for (var i = 0; i < entries.Length; i++)
                tasks[i] = RunChecklistWatchdogForEntryAsync(entries[i], sentChecklists, cancellationToken);
            
            var resultTask =
                Task.Factory.ContinueWhenAll(tasks, _ => sentChecklists.CompleteAdding(), cancellationToken);
            
            while (sentChecklists.TryTake(out var sentChecklist, -1, cancellationToken))
                yield return sentChecklist;

            if (!resultTask.IsCompleted)
                await resultTask.ConfigureAwait(false);
            else if (resultTask.IsFaulted)
                // ReSharper disable once PossibleNullReferenceException
                throw resultTask.Exception;
        }

        private async Task RunChecklistWatchdogForEntryAsync(ChecklistWatchdogEntry entry,
            BlockingCollection<SentChecklist> sentChecklists, CancellationToken cancellationToken)
        {
            var restaurant = _data.Restaurants.Find(r => r.ChatId == entry.PublishChatId) ?? _data.Restaurants[0];
            if (restaurant == null)
                return;

            var sendAtDaysOfWeek = entry.PublishDaysOfWeek?.Where(dow => dow > 0 && dow < 8)
                .Select(dow => dow == 7 ? DayOfWeek.Sunday : (DayOfWeek) dow);

            TimeSpan GetDelay(out DayOfWeek todayIsDayOfWeek, out int todayIsDayOfMonth)
            {
                var now = _cultureService.NowFor(restaurant);
                todayIsDayOfWeek = now.DayOfWeek;
                todayIsDayOfMonth = now.Day;
                return now.TimeOfDay < entry.SendAt
                    ? entry.SendAt - now.TimeOfDay
                    : entry.SendAt + TimeSpan.FromDays(1d) - now.TimeOfDay;
            }
            
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(GetDelay(out var todayIsDayOfWeek, out var todayIsDayOfMonth), cancellationToken).ConfigureAwait(false);

                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (entry.Type)
                {
                    case ChecklistWatchdogEntryType.Weekly:
                    {
                        if (!sendAtDaysOfWeek.Contains(todayIsDayOfWeek))
                            continue;
                        break;
                    }

                    case ChecklistWatchdogEntryType.Monthly:
                    {
                        if (entry.PublishDays.Any(expectedRangeOfDays =>
                            expectedRangeOfDays.Start.Value < todayIsDayOfMonth ||
                            expectedRangeOfDays.End.Value > todayIsDayOfMonth))
                            continue;
                        
                        break;
                    }
                }

                try
                {
                    var totalAttachmentsFound = 0;
                    await foreach (var messageId in _mailClient.ExecuteForRestaurantAsync(restaurant, entry.ChecklistName,
                        entry.ChecklistMessage, cancellationToken))
                    {
                        var sentChecklist = new SentChecklist
                        {
                            Date = _cultureService.NowFor(restaurant).Date,
                            MessageId = messageId,
                            ChecklistName = entry.ChecklistName
                        };
                        sentChecklists.TryAdd(sentChecklist, -1, cancellationToken);
                        totalAttachmentsFound++;
                    }

                    if (totalAttachmentsFound == 0)
                    {
                        if (entry.NotifyNotFoundChatId != 0L && !string.IsNullOrWhiteSpace(entry.NotifyNotFoundMessage))
                            await _client.SendTextMessageAsync(entry.NotifyNotFoundChatId, entry.NotifyNotFoundMessage,
                                disableWebPagePreview: true, cancellationToken: cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        if (entry.NotifyFoundChatId != 0L && !string.IsNullOrWhiteSpace(entry.NotifyFoundMessage))
                            await _client.SendTextMessageAsync(entry.NotifyFoundChatId, entry.NotifyFoundMessage,
                                disableWebPagePreview: true, cancellationToken: cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
        
        private sealed class ChecklistWatchdogEntry
        {
            public ChecklistWatchdogEntryType Type;
            public string ChecklistName;
            public string ChecklistMessage;
            public TimeSpan SendAt;
            public int[] PublishDaysOfWeek;
            public Range[] PublishDays;
            public long PublishChatId;
            public long NotifyFoundChatId;
            public string NotifyFoundMessage;
            public long NotifyNotFoundChatId;
            public string NotifyNotFoundMessage;
        }
        
        private enum ChecklistWatchdogEntryType
        {
            Daily, Weekly, Monthly
        }
    }
}