using MongoDB.Bson;

namespace MaximEmmBots.Models.Mongo
{
    internal sealed class SentStat
    {
        public ObjectId Id { get; set; }
        
        public string StatId { get; set; }
        
        public string SentDate { get; set; }
    }
}