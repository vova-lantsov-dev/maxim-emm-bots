namespace MaximEmmBots.Models.Json
{
    internal sealed class BotData
    {
        public string Token { get; set; }
        
        public string Username { get; set; }
        
        public string GroupReloadCommand { get; set; }
        
        public string PrivateReloadCommand { get; set; }
    }
}