namespace NovaWallet.Models;

public class AuditLogEntry
{
    public Guid Id { get; set; }

    public string EntityType { get; set; } = default!; 
    public Guid EntityId { get; set; }
    public string Action { get; set; } = default!;    

    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? ActorId { get; set; }

    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}