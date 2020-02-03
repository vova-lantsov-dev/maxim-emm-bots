using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MaximEmmBots.Models.Json;
using MaximEmmBots.Models.Json.Restaurants;

namespace MaximEmmBots.Extensions
{
    internal static class SettingsExtensions
    {
        private static readonly string BasePath = Directory.GetCurrentDirectory();
        
        internal static async Task<Data> LoadDataAsync(bool isDevelopment)
        {
            var settingsFilePath = Path.Combine(BasePath, !isDevelopment ? "settings.json" : "settings.Development.json");
            await using var settingsFile = File.OpenRead(settingsFilePath);
            return await JsonSerializer.DeserializeAsync<Data>(settingsFile);
        }

        internal static async IAsyncEnumerable<Restaurant> YieldRestaurantsAsync(bool isDevelopment)
        {
            var dirPath = Path.Combine(BasePath, "Restaurants");
            foreach (var filePath in Directory.GetFiles(dirPath, "*.json", SearchOption.AllDirectories)
                .Where(file => file.EndsWith("Development.json") == isDevelopment))
            {
                await using var fileWithRestaurant = File.OpenRead(filePath);
                var restaurant = await JsonSerializer.DeserializeAsync<Restaurant>(fileWithRestaurant);
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
                var languageModel = await JsonSerializer.DeserializeAsync<LocalizationModel>(languageFile);
                yield return (languageName, languageModel);
            }
        }
    }
}