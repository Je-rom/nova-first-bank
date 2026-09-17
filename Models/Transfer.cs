namespace NovaWallet.Models;

public class Transfer
{
    public Guid Id { get; set; }
    public Guid FromWalletId { get; set; }
    public Guid ToWalletId { get; set; }
    public long AmountKobo { get; set; }
    public string Currency { get; set; } = "NGN";

    // Unique constraint on this column (configured in DbContext) is what makes
    // idempotency actually safe under concurrent replays — only one INSERT
    // with the same key can succeed.
    public string IdempotencyKey { get; set; } = default!;

    // Hash of (FromWalletId, ToWalletId, AmountKobo). Same key + same hash =
    // safe replay, return the stored result. Same key + different hash = 409.
    public string RequestHash { get; set; } = default!;

    public TransferStatus Status { get; set; } = TransferStatus.Pending;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public string? FailureReason { get; set; }
}