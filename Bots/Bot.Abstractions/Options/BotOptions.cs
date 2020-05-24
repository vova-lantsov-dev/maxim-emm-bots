namespace Bot.Abstractions.Options
{
    public sealed class BotOptions
    {
        public string Token { get; set; }
        public string Username { get; set; }
        public string GroupReloadCommand { get; set; }
        public string PrivateReloadCommand { get; set; }
    }
}