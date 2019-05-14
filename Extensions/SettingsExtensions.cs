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
    }
}