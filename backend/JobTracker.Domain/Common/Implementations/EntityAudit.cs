using System;

namespace JobTracker.Domain;

public abstract class EntityAudit : Entity, IAuditable, ISoftDeletable
{
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsArchived { get; set; }
}