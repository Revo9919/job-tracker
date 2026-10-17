using System.ComponentModel.DataAnnotations;

namespace JobTracker.Domain;

public abstract class Entity
{
    [Key]
    public int Id { get; set; }
}