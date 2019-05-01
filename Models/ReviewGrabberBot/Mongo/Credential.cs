using MongoDB.Bson.Serialization.Attributes;

namespace MaximEmmBots.Models.ReviewGrabberBot.Mongo
{
    [BsonIgnoreExtraElements]
    internal sealed class Credential
    {
        [BsonElement("name")]
        public string Name { get; set; }
        
        [BsonElement("access_token")]
        public string AccessToken { get; set; }
    }
}