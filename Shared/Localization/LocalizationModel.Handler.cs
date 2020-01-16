using MaximEmm.Shared.Abstractions;

namespace MaximEmm.Shared
{
    public sealed partial class LocalizationModel : IHandlerLocalization
    {
        public string ResponseToReviewSent { get; set; }
        
        public string NewMemberInGroup { get; set; }
        
        public string NewMemberForAdmin { get; set; }
    }
}