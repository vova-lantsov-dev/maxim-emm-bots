using System.Collections.Generic;
using System.Threading;

namespace MaximEmmBots.Services.MailBot
{
    internal interface IMailClient
    {
        IAsyncEnumerable<string> ExecuteForRestaurantAsync(long chatId, string checklistName,
            string notifyMessage, CancellationToken cancellationToken);
    }
}