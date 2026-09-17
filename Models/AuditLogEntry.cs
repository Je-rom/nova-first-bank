namespace NovaWallet.Models;

// Append-only audit trail, separate from WalletTransaction (the statement
// table) per the brief. No code path should ever call SaveChanges after
// mutating one of these — only ever Add + SaveChanges. Consider also locking
// this down at the DB level later (e.g. REVOKE UPDATE/DELETE) as a stretch item.
public class AuditLogEntry
{
    public Guid Id { get; set; }

    public string EntityType { get; set; } = default!;   // "Wallet", "Transfer"
    public Guid EntityId { get; set; }
    public string Action { get; set; } = default!;        // "Credit", "DebitTransfer", "WalletCreated", etc.

    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? ActorId { get; set; }

    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}