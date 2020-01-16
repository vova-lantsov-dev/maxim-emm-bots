using MaximEmm.Shared.Abstractions;

namespace MaximEmm.Shared
{
    public partial class LocalizationModel : IMailingLocalization
    {
        public string TimeBoardIsNotAvailableForThisMonth { get; set; }

        public string WhoWorksAtDate { get; set; }

        public string WhoWorksWithYou { get; set; }

        public string YouWorkAt { get; set; }

        public string TimeForUserWithTelegram { get; set; }

        public string TimeForUserWithoutTelegram { get; set; }
    }
}
