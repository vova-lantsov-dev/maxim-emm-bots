using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using MaximEmmBots.Extensions;
using MaximEmmBots.Models.Json.Restaurants;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types.InputFiles;

// ReSharper disable LoopCanBeConvertedToQuery

namespace MaximEmmBots.Services.MailBot
{
    internal sealed class GmailClient : IMailClient
    {
        private readonly GmailService _gmailService;
        private readonly ITelegramBotClient _botClient;
        private readonly CultureService _cultureService;
        private readonly ILogger<GmailClient> _logger;
        
        public GmailClient(GmailService gmailService,
            ITelegramBotClient botClient,
            CultureService cultureService,
            ILogger<GmailClient> logger)
        {
            _gmailService = gmailService;
            _botClient = botClient;
            _cultureService = cultureService;
            _logger = logger;
        }

        public async IAsyncEnumerable<string> ExecuteForRestaurantAsync(Restaurant restaurant,
            CancellationToken cancellationToken)
        {
            const string userId = "me";

            var result = await _gmailService.Users.Threads.List(userId).ExecuteAsync(cancellationToken);

            foreach (var gmailThread in result.Threads.Reverse())
            {
                var threadInfo = await _gmailService.Users.Threads.Get(userId, gmailThread.Id)
                    .ExecuteAsync(cancellationToken);

                foreach (var gmailThreadMessage in threadInfo.Messages)
                {
                    var messageInfo = await _gmailService.Users.Messages.Get(userId, gmailThreadMessage.Id)
                        .ExecuteAsync(cancellationToken);

                    MessagePart GetAttachmentPart(MessagePart part)
                    {
                        if (part.Parts == null)
                            return null;

                        foreach (var innerPart in part.Parts)
                        {
                            if (!string.IsNullOrEmpty(innerPart.MimeType))
                                continue;

                            return innerPart;
                        }

                        foreach (var innerPart in part.Parts)
                        {
                            var found = GetAttachmentPart(innerPart);
                            if (found != null)
                                return found;
                        }

                        return null;
                    }

                    var attachmentPart = GetAttachmentPart(messageInfo.Payload);
                    if (attachmentPart == null ||
                        !attachmentPart.Filename.Contains(restaurant.Name, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var pdfAttachment = await _gmailService.Users.Messages.Attachments.Get(userId, messageInfo.Id,
                        attachmentPart.Body.AttachmentId).ExecuteAsync(cancellationToken);
                    var pdfAttachmentBytes = Convert.FromBase64String(pdfAttachment.Data.ToBase64Url());
                    await using var pdfAttachmentStream = new MemoryStream(pdfAttachmentBytes);

                    await _botClient.SendDocumentAsync(restaurant.ChatId,
                        new InputOnlineFile(pdfAttachmentStream, attachmentPart.Filename.ToFileName()),
                        DateTime.UnixEpoch.AddMilliseconds(messageInfo.InternalDate ?? 0L)
                            .ToString("f", _cultureService.CultureFor(restaurant)),
                        cancellationToken: cancellationToken);

                    yield return null;
                }
            }
        }
    }
}