namespace CRMWebApp.Models
{
    public interface IOwnedEntity
    {
        string? UserId { get; }
        string? CreatedById { get; }
        bool IsDeleted { get; }
    }
}
