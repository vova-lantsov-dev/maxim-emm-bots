using System.Collections.Generic;
using System.Threading;
using MaximEmmBots.Models.Json.Restaurants;

namespace MaximEmmBots.Services.MailBot
{
    internal interface IMailClient
    {
        IAsyncEnumerable<string> ExecuteForRestaurantAsync(Restaurant restaurant, CancellationToken cancellationToken);
    }
}