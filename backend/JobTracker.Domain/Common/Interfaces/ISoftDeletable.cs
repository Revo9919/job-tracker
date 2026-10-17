namespace JobTracker.Domain;

public interface ISoftDeletable
{
    bool IsArchived { get; set; }
}