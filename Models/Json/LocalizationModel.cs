namespace MaximEmmBots.Models.Json
{
    internal sealed class LocalizationModel
    {
        #region DistributionBotModel
        
        public string TimeBoardIsNotAvailableForThisMonth { get; set; }
        
        public string WhoWorksAtDate { get; set; }
        
        public string WhoWorksWithYou { get; set; }
        
        public string YouWorkAt { get; set; }
        
        public string TimeForUserWithTelegram { get; set; }
        
        public string TimeForUserWithoutTelegram { get; set; }
        
        #endregion

        #region ReviewBotModel

        public string ViewFeedback { get; set; }
        
        public string OpenReview { get; set; }
        
        public string Comments { get; set; }

        #endregion

        #region ReviewModel

        public string RestaurantAndSourceForReview { get; set; }
        
        public string TypeForReview { get; set; }
        
        public string RatingForReview { get; set; }
        
        public string TextForReview { get; set; }

        #endregion

        #region BotHandler

        public string ResponseToReviewSent { get; set; }
        
        public string NewMemberInGroup { get; set; }
        
        public string NewMemberForAdmin { get; set; }

        #endregion

        #region StatsBot

        public string StatsForPeriod { get; set; }

        #endregion
    }
}