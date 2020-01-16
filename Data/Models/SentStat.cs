using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MaximEmm.Data.Models
{
    public sealed class SentStat
    {
        public ObjectId Id { get; set; }
        
        public string StatId { get; set; }
        
        [BsonDateTimeOptions(DateOnly = true, Kind = DateTimeKind.Unspecified)]
        public DateTime SentDate { get; set; }
    }
}