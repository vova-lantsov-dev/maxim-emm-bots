using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MaximEmmBots.Models.Json;

namespace MaximEmmBots.Extensions
{
    internal static class SettingsExtensions
    {
        internal static async Task<Data> LoadDataAsync(string settingsFilePath)
        {
            await using var settingsFile = File.OpenRead(settingsFilePath);
            return await JsonSerializer.ReadAsync<Data>(settingsFile);
        }

        internal static async IAsyncEnumerable<(string name, LocalizationModel model)> YieldLanguagesAsync(string basePath, IEnumerable<string> languageKeys)
        {
            foreach (var languageKey in languageKeys)
            {
                await using var languageFile = File.OpenRead(Path.Combine(basePath, languageKey + ".json"));
                yield return (languageKey, await JsonSerializer.ReadAsync<LocalizationModel>(languageFile));
            }
        }
    }
}