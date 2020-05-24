using Bot.Abstractions.Data.Models;
using Bot.Abstractions.Options;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Bot.Abstractions.Data
{
    public sealed class BotContext
    {
        public readonly IMongoCollection<Review> Reviews;
        public readonly IMongoCollection<GoogleReviewMessage> GoogleReviewMessages;
        public readonly IMongoCollection<Credential> GoogleCredentials;
        public readonly IMongoCollection<SentForm> SentForms;
        public readonly IMongoCollection<SentStat> SentStats;
        public readonly IMongoCollection<UserRestaurantPair> UserRestaurantPairs;
        public readonly IMongoCollection<SentChecklist> SentChecklists;
        public readonly IMongoCollection<HealthCheckEntry> HealthChecks;
        
        public BotContext(IOptions<MongoOptions> options)
        {
            var mongoClient = new MongoClient(options.Value.ConnectionString);
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