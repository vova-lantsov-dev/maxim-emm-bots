using MaximEmm.Data.Models;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace MaximEmm.Data
{
    public sealed class BotsDatabaseContext
    {
        public readonly IMongoCollection<Review> Reviews;
        public readonly IMongoCollection<GoogleReviewMessage> GoogleReviewMessages;
        public readonly IMongoCollection<Credential> GoogleCredentials;
        public readonly IMongoCollection<SentForm> SentForms;
        public readonly IMongoCollection<SentStat> SentStats;
        public readonly IMongoCollection<UserRestaurantPair> UserRestaurantPairs;
        public readonly IMongoCollection<SentChecklist> SentChecklists;
        public readonly IMongoCollection<HealthCheckEntry> HealthChecks;

        public BotsDatabaseContext(IConfiguration configuration)
        {
            var mongoConnectionString = configuration["DB_CONNECTION_STRING"] ?? "mongodb://localhost";

            var mongoClient = new MongoClient(mongoConnectionString);
            var db = mongoClient.GetDatabase("reviewbot");

            Reviews = db.GetCollection<Review>("reviews");
            GoogleReviewMessages = db.GetCollection<GoogleReviewMessage>(nameof(GoogleReviewMessages));
            GoogleCredentials = db.GetCollection<Credential>("credentials");
            SentForms = db.GetCollection<SentForm>(nameof(SentForms));
            SentStats = db.GetCollection<SentStat>(nameof(SentStats));
            UserRestaurantPairs = db.GetCollection<UserRestaurantPair>(nameof(UserRestaurantPairs));
            SentChecklists = db.GetCollection<SentChecklist>(nameof(SentChecklists));
            HealthChecks = db.GetCollection<HealthCheckEntry>("health_check");
        }
    }
}
