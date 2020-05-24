using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Bot.Abstractions.Data.Models
{
    public sealed class SentChecklist
    {
        public ObjectId Id { get; set; }
        
        public string MessageId { get; set; }
        
        public string ChecklistName { get; set; }
        
        [BsonDateTimeOptions(Kind = DateTimeKind.Unspecified, DateOnly = true)]
        public DateTime Date { get; set; }
    }
}