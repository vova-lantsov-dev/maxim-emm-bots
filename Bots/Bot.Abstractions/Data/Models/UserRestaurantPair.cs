using MongoDB.Bson;

namespace Bot.Abstractions.Data.Models
{
    public sealed class UserRestaurantPair
    {
        public ObjectId Id { get; set; }
        
        public int UserId { get; set; }
        
        public long RestaurantId { get; set; }
    }
}