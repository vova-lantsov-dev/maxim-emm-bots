using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MaximEmmBots.Models.Mongo
{
    internal sealed class SentCounter
    {
        public ObjectId Id { get; set; }
        
        public string Date { get; set; }
        
        public long RestaurantId { get; set; }
        
        [BsonIgnoreIfNull]
        public List<string> SentTimes { get; set; }
    }
}