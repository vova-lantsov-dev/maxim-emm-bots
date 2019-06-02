using MongoDB.Bson;

namespace MaximEmmBots.Models.Mongo
{
    internal sealed class UserRestaurantPair
    {
        public ObjectId Id { get; set; }
        
        public int UserId { get; set; }
        
        public long RestaurantId { get; set; }
    }
}