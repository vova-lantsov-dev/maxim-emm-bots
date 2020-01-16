namespace MaximEmm.Shared.Abstractions
{
    public interface IMailingLocalization
    {
        string TimeBoardIsNotAvailableForThisMonth { get; }

        string WhoWorksAtDate { get; }

        string WhoWorksWithYou { get; }

        string YouWorkAt { get; }

        string TimeForUserWithTelegram { get; }

        string TimeForUserWithoutTelegram { get; }
    }
}