using MongoDB.Bson.Serialization.Attributes;

namespace Bot.Abstractions.Data.Models
{
    [BsonIgnoreExtraElements]
    public sealed class Credential
    {
        [BsonElement("name")]
        public string Name { get; set; }
        
        [BsonElement("access_token")]
        public string AccessToken { get; set; }
    }
}