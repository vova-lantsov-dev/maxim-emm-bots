using MaximEmmBots.Models.Mongo;
using MaximEmmBots.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

// ReSharper disable SuggestBaseTypeForParameter

namespace MaximEmmBots.Services
{
    internal sealed class Context
    {
        internal readonly IMongoCollection<Review> Reviews;
        internal readonly IMongoCollection<GoogleReviewMessage> GoogleReviewMessages;
        internal readonly IMongoCollection<Credential> GoogleCredentials;
        internal readonly IMongoCollection<SentForm> SentForms;
        internal readonly IMongoCollection<SentStat> SentStats;
        internal readonly IMongoCollection<UserRestaurantPair> UserRestaurantPairs;
        
        public Context(IOptions<DataOptions> options, ILogger<Context> logger)
        {
            logger.LogTrace("Context initialization started");
            
            var mongoClient = new MongoClient(options.Value.Data.MongoConnectionString);
            var db = mongoClient.GetDatabase("reviewbot");

            Reviews = db.GetCollection<Review>("reviews");
            GoogleReviewMessages = db.GetCollection<GoogleReviewMessage>(nameof(GoogleReviewMessages));
            GoogleCredentials = db.GetCollection<Credential>("credentials");
            SentForms = db.GetCollection<SentForm>(nameof(SentForms));
            SentStats = db.GetCollection<SentStat>(nameof(SentStats));
            UserRestaurantPairs = db.GetCollection<UserRestaurantPair>(nameof(UserRestaurantPairs));
            
            logger.LogTrace("Context initialization finished");
        }
    }
}