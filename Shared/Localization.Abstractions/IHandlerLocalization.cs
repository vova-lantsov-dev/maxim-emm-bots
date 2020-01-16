namespace MaximEmm.Shared.Abstractions
{
    public interface IHandlerLocalization
    {
        string ResponseToReviewSent { get; }
        
        string NewMemberInGroup { get; }
        
        string NewMemberForAdmin { get; }
    }
}