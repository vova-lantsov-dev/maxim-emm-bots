using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MaximEmmBots.Serializers
{
    internal sealed class RatingMongoSerializer : SerializerBase<int>
    {
        public override int Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (context.Reader.CurrentBsonType)
            {
                case BsonType.Int32:
                    return context.Reader.ReadInt32();
                case BsonType.String:
                    return int.TryParse(context.Reader.ReadString(), out var num) ? num : -1;
                case BsonType.Double:
                    return (int) context.Reader.ReadDouble();
                default:
                    throw new FormatException($"The type of the rating value is {context.Reader.CurrentBsonType}");
            }
        }
    }
}