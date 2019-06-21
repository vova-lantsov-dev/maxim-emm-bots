using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MaximEmmBots.Models.Mongo
{
    internal sealed class SentChecklist
    {
        public ObjectId Id { get; set; }
        
        public string ChecklistName { get; set; }
        
        [BsonDateTimeOptions(Kind = DateTimeKind.Unspecified, DateOnly = true)]
        public DateTime Date { get; set; }
    }
}