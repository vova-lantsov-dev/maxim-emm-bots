using System;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using MaximEmm.Data.Models;
using MaximEmm.Shared.Abstractions;

namespace MaximEmm.Bots.Extensions
{
    internal static class ReviewExtensions
    {
        internal static string ToString(
            this Review review,
            IFormatProvider culture,
            IReviewLocalization model,
            int maxCountOfStars,
            bool preferAvatarOverProfileLink)
        {
            var result = new StringBuilder();

            result.AppendFormat(culture, model.RestaurantAndSourceForReview, review.RestaurantName, review.Resource);

            if (review.ReviewType != null)
            {
                result.Append('\n');
                result.AppendFormat(culture, model.TypeForReview, review.ReviewType);
            }

            var link = !preferAvatarOverProfileLink
                ? review.ProfileUrl ?? review.AuthorAvatar
                : review.AuthorAvatar ?? review.ProfileUrl;
            result.AppendFormat(culture, "\n{0} <i>({1})</i>",
                string.IsNullOrWhiteSpace(link) ? review.AuthorName : $"<a href=\"{link}\">{review.AuthorName}</a>", review.Date);

            if (review.Rating > 0)
            {
                result.Append('\n');
                result.Append(model.RatingForReview);
                result.AppendJoin(string.Empty, Enumerable.Repeat("👍", review.Rating));

                var emptyStarsCount = maxCountOfStars - review.Rating;
                if (emptyStarsCount > 0)
                    result.AppendJoin(string.Empty, Enumerable.Repeat("👍🏿", emptyStarsCount));
            }

            if (review.Likes > 0)
            {
                result.AppendFormat(culture, "\n{0} {1}", review.Likes, review.Dislikes <= 0 ? "❤️" : "👍");

                if (review.Dislikes > 0)
                    result.AppendFormat(culture, "\n{0} 👎", review.Dislikes);
            }

            if (!string.IsNullOrWhiteSpace(review.Text))
            {
                result.Append('\n');
                var text = new string(review.Text.Take(1021 - result.Length).ToArray());
                if (review.Text.Length > text.Length)
                {
                    text += "...";
                }
                result.AppendFormat(model.TextForReview, HtmlEncoder.Default.Encode(text));
            }

            return result.ToString();
        }
    }
}