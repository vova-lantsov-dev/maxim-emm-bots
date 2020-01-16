namespace MaximEmm.Shared.Abstractions
{
    public interface IReviewLocalization
    {
        string ViewFeedback { get; }

        string OpenReview { get; }

        string Comments { get; }
        
        string RestaurantAndSourceForReview { get; }

        string TypeForReview { get; }

        string RatingForReview { get; }

        string TextForReview { get; }
    }
}