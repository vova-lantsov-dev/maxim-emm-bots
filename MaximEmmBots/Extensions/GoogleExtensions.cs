using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Sheets.v4;
using Google.Apis.Util.Store;
using MaximEmmBots.Models.Json;

namespace MaximEmmBots.Extensions
{
    internal static class GoogleExtensions
    {
        internal static Task<UserCredential> AuthorizeAsync(GoogleCredentials googleCredentials)
        {
            return GoogleWebAuthorizationBroker.AuthorizeAsync(
                new ClientSecrets
                {
                    ClientId = googleCredentials.ClientId,
                    ClientSecret = googleCredentials.ClientSecret
                },
                new[] {SheetsService.Scope.SpreadsheetsReadonly, GmailService.Scope.GmailReadonly},
                "user", default,
                new FileDataStore("credentials", true));
        }
    }
}