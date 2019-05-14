using MaximEmmBots.Models.Mongo;
using MaximEmmBots.Options;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace MaximEmmBots.Services
{
    internal sealed class Context
    {
        internal readonly IMongoCollection<Review> Reviews;
        internal readonly IMongoCollection<GoogleReviewMessage> GoogleReviewMessages;
        internal readonly IMongoCollection<Credential> GoogleCredentials;
        
        public Context(IOptions<DataOptions> options)
        {
            var data = options.Value.Data.Database;
            var mongoClient = new MongoClient(data.ConnectionString);
            var db = mongoClient.GetDatabase("reviewbot");

            Reviews = db.GetCollection<Review>("reviews");
            GoogleReviewMessages = db.GetCollection<GoogleReviewMessage>(nameof(GoogleReviewMessages));
            GoogleCredentials = db.GetCollection<Credential>("credentials");
        }
    }
}