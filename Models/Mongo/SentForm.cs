using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;

namespace MaximEmmBots.Models.Mongo
{
    internal sealed class SentForm
    {
        public ObjectId Id { get; set; }
        
        [BsonDateTimeOptions(DateOnly = true, Kind = DateTimeKind.Unspecified)]
        public DateTime Date { get; set; }
        
        public long RestaurantId { get; set; }
        
        public List<SentFormItem> Items { get; set; }
    }

    internal sealed class SentFormItem
    {
        [BsonTimeSpanOptions(BsonType.Int64, TimeSpanUnits.Seconds)]
        public TimeSpan SentTime { get; set; }
        
        public string EmployeeName { get; set; }
    }
}