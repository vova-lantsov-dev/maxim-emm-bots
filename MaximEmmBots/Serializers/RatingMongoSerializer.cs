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
            // TODO local scope visibility bug (watching https://youtrack.jetbrains.com/issue/RIDER-27874)
            int ParseString(string text) => int.TryParse(text, out var value) ? value : -1;
            
            return context.Reader.CurrentBsonType switch
            {
                BsonType.Int32 => context.Reader.ReadInt32(),
                BsonType.String => ParseString(context.Reader.ReadString()),
                BsonType.Double => (int) context.Reader.ReadDouble(),
                _ => throw new FormatException($"The type of the rating value is {context.Reader.CurrentBsonType}")
            };
        }
    }
}