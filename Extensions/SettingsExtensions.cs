using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MaximEmmBots.Models.Json;
using MaximEmmBots.Models.Json.Restaurants;

namespace MaximEmmBots.Extensions
{
    internal static class SettingsExtensions
    {
        private static readonly string BasePath = Directory.GetCurrentDirectory();
        
        internal static async Task<Data> LoadDataAsync()
        {
            var settingsFilePath = Path.Combine(BasePath, "settings.json");
            await using var settingsFile = File.OpenRead(settingsFilePath);
            return await JsonSerializer.ReadAsync<Data>(settingsFile);
        }

        internal static async IAsyncEnumerable<Restaurant> YieldRestaurantsAsync()
        {
            var dirPath = Path.Combine(BasePath, "Restaurants");
            foreach (var filePath in Directory.GetFiles(dirPath, "*.json", SearchOption.AllDirectories))
            {
                await using var fileWithRestaurant = File.OpenRead(filePath);
                var restaurant = await JsonSerializer.ReadAsync<Restaurant>(fileWithRestaurant);
                restaurant.Name = Path.GetFileNameWithoutExtension(filePath);
                yield return restaurant;
            }
        }

        internal static async IAsyncEnumerable<(string name, LocalizationModel model)> YieldLanguagesAsync()
        {
            var dirPath = Path.Combine(BasePath, "Languages");
            foreach (var languageFilePath in Directory.GetFiles(dirPath, "*.json", SearchOption.AllDirectories))
            {
                await using var languageFile = File.OpenRead(languageFilePath);
                var languageName = Path.GetFileNameWithoutExtension(languageFilePath);
                var languageModel = await JsonSerializer.ReadAsync<LocalizationModel>(languageFile);
                yield return (languageName, languageModel);
            }
        }
    }
}