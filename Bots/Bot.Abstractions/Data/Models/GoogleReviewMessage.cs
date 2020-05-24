using MongoDB.Bson.Serialization.Attributes;

namespace Bot.Abstractions.Data.Models
{
    public sealed class GoogleReviewMessage
    {
        [BsonId]
        public string ReviewId { get; set; }

        public int MessageId { get; set; }

        public long ChatId { get; set; }
    }
}