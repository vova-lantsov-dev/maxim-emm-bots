using MongoDB.Bson.Serialization.Attributes;

namespace MaximEmmBots.Models.ReviewGrabberBot.Mongo
{
    internal sealed class GoogleReviewMessage
    {
        [BsonId]
        public string ReviewId { get; set; }

        public int MessageId { get; set; }

        public long ChatId { get; set; }
    }
}