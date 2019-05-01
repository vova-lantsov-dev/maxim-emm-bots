using Newtonsoft.Json;

namespace MaximEmmBots.Models.ReviewGrabberBot.Json
{
    internal sealed class GoogleCommentModel
    {
        [JsonProperty("comment")]
        public string Comment { get; set; }
    }
}