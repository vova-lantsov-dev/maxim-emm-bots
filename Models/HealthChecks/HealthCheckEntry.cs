using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace MaximEmmBots.Models.HealthChecks
{
    public sealed class HealthCheckEntry
    {
        public ObjectId Id { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("last_check")]
        public DateTime LastCheck { get; set; }

        [BsonElement("tests_passed")]
        public bool TestsPassed { get; set; }

        [BsonElement("uris")]
        public Dictionary<string, HealthCheckUriItem> Uris { get; set; }
    }

    public sealed class HealthCheckUriItem
    {
        [BsonElement("restaurant_name")]
        public string RestaurantName { get; set; }

        [BsonElement("tests_passed")]
        public bool TestsPassed { get; set; }

        [BsonElement("tests")]
        public List<HealthCheckTestItem> Tests { get; set; }

        [BsonElement("success_items_scraped")]
        public int SuccessItemsScraped { get; set; }
    }

    public sealed class HealthCheckTestItem
    {
        [BsonElement("status")]
        public string Status { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }
    }
}
