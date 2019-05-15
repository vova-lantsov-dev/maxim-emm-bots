using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Util.Store;
using MaximEmmBots.Models.Json.Distribution;

namespace MaximEmmBots.Extensions
{
    internal static class GoogleSheetsExtensions
    {
        internal static Task<UserCredential> AuthorizeAsync(SpreadsheetData spreadsheetData)
        {
            return GoogleWebAuthorizationBroker.AuthorizeAsync(
                new ClientSecrets
                {
                    ClientId = spreadsheetData.ClientId,
                    ClientSecret = spreadsheetData.ClientSecret
                },
                new[] {SheetsService.Scope.SpreadsheetsReadonly},
                "user", default,
                new FileDataStore("credentials", true));
        }
    }
}