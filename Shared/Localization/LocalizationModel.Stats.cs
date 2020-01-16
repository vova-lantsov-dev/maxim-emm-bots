using MaximEmm.Shared.Abstractions;

namespace MaximEmm.Shared
{
    public partial class LocalizationModel : IStatsLocalization
    {
        public string StatsForPeriod { get; set; }
    }
}
