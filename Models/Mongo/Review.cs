using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MaximEmmBots.Models.Json;
using MaximEmmBots.Serializers;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

// ReSharper disable InvertIf

namespace MaximEmmBots.Models.Mongo
{
    [BsonIgnoreExtraElements]
    internal sealed class Review
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        
        [BsonElement("resource")]
        public string Resource { get; set; }
        
        [BsonElement("restaurant_name")]
        public string RestaurantName { get; set; }

        [BsonElement("need_to_show")]
        public bool NeedToShow { get; set; }

        [BsonElement("reply_link")]
        public string ReplyLink { get; set; }

        [BsonElement("author_name")]
        public string AuthorName { get; set; }

        [BsonElement("author_avatar")]
        public string AuthorAvatar { get; set; }

        [BsonElement("photos")]
        public List<string> Photos { get; set; }

        [BsonElement("rating"), BsonSerializer(typeof(RatingMongoSerializer))]
        public int Rating { get; set; }

        [BsonElement("date")]
        public string Date { get; set; }

        [BsonElement("text")]
        public string Text { get; set; }

        [BsonElement("is_readonly")]
        public bool IsReadOnly { get; set; }

        [BsonElement("comments")]
        public List<string> Comments { get; set; }

        [BsonElement("likes")]
        public int Likes { get; set; }

        [BsonElement("dislikes")]
        public int Dislikes { get; set; }

        [BsonElement("profile_link")]
        public string ProfileUrl { get; set; }

        [BsonElement("type")]
        public string ReviewType { get; set; }

        public string ToString(LocalizationModel model, int maxCountOfStars, bool preferAvatarOverProfileLink)
        {
            var result = new StringBuilder();

            result.AppendFormat(model.RestaurantAndSourceForReview, RestaurantName, Resource);

            if (ReviewType != null)
            {
                result.Append('\n');
                result.AppendFormat(model.TypeForReview, ReviewType);
            }

            var link = !preferAvatarOverProfileLink ? ProfileUrl ?? AuthorAvatar : AuthorAvatar ?? ProfileUrl;
            result.AppendFormat("\n{0} _({1})_",
                string.IsNullOrWhiteSpace(link) ? AuthorName : $"[{AuthorName}]({link})", Date);

            if (Rating > 0)
            {
                result.Append('\n');
                result.Append(model.RatingForReview);
                result.AppendJoin(string.Empty, Enumerable.Repeat("👍", Rating));

                var emptyStarsCount = maxCountOfStars - Rating;
                if (emptyStarsCount > 0)
                    result.AppendJoin(string.Empty, Enumerable.Repeat("👍🏿", emptyStarsCount));
            }

            if (Likes > 0)
            {
                result.AppendFormat("\n{0} {1}", Likes, Dislikes <= 0 ? "❤️" : "👍");

                if (Dislikes > 0)
                    result.AppendFormat("\n{0} 👎", Dislikes);
            }

            if (!string.IsNullOrWhiteSpace(Text))
            {
                result.Append('\n');
                result.AppendFormat(model.TextForReview, Regex.Replace(Text,
                    "(?<token>[*_\\\\`\\\\[\\]])",
                    m => $"\\{m.Groups["token"].Value}"));
            }

            return result.ToString();
        }
    }
}