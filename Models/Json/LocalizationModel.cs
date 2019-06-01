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
    }
}