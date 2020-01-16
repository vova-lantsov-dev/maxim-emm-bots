using MaximEmm.Shared.Abstractions;

namespace MaximEmm.Shared
{
    public partial class LocalizationModel : IReviewLocalization
    {
        public string ViewFeedback { get; set; }

        public string OpenReview { get; set; }

        public string Comments { get; set; }
        
        public string RestaurantAndSourceForReview { get; set; }

        public string TypeForReview { get; set; }

        public string RatingForReview { get; set; }

        public string TextForReview { get; set; }
    }
}
