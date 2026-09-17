namespace NovaWallet.Models;

public class Transfer
{
    public Guid Id { get; set; }
    public Guid FromWalletId { get; set; }
    public Guid ToWalletId { get; set; }
    public long AmountKobo { get; set; }
    public string Currency { get; set; } = "NGN";
    public string IdempotencyKey { get; set; } = default!;
    public string RequestHash { get; set; } = default!;
    public TransferStatus Status { get; set; } = TransferStatus.Pending;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public string? FailureReason { get; set; }
}