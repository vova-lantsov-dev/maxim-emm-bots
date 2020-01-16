using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System;

namespace MaximEmm.Data.Serializers
{
    internal sealed class RatingSerializer : SerializerBase<int>
    {
        public override int Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            return context.Reader.CurrentBsonType switch
            {
                BsonType.Int32 => context.Reader.ReadInt32(),
                BsonType.String => int.TryParse(context.Reader.ReadString(), out var value) ? value : -1,
                BsonType.Double => (int)context.Reader.ReadDouble(),
                _ => throw new FormatException($"The type of the rating value is {context.Reader.CurrentBsonType}")
            };
        }
    }
}
