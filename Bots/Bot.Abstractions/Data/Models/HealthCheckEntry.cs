using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace Bot.Abstractions.Data.Models
{
    [BsonIgnoreExtraElements]
    public sealed class HealthCheckEntry
    {
        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("last_check")]
        public DateTime LastCheck { get; set; }

        [BsonElement("tests_passed")]
        public bool TestsPassed { get; set; }

        [BsonElement("uris")]
        public Dictionary<string, HealthCheckUriItem> Uris { get; set; }
    }

    [BsonIgnoreExtraElements]
    public sealed class HealthCheckUriItem
    {
        [BsonElement("restaurant_name")]
        public string RestaurantName { get; set; }

        [BsonElement("tests_passed")]
        public bool TestsPassed { get; set; }

        [BsonElement("tests")]
        public List<HealthCheckTestItem> Tests { get; set; }

        [BsonElement("success_items_scraped")]
        public int? SuccessItemsScraped { get; set; }
    }

    [BsonIgnoreExtraElements]
    public sealed class HealthCheckTestItem
    {
        [BsonElement("status")]
        public string Status { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }
    }
}
