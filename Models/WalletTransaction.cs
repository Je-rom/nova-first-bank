namespace NovaWallet.Models;

public class WalletTransaction
{
    public Guid Id { get; set; }
    public Guid WalletId { get; set; }
    public Wallet? Wallet { get; set; }

    public TransactionType Type { get; set; }
    public long AmountKobo { get; set; }
    public string Currency { get; set; } = "NGN";

    // Wallet balance immediately after this entry was applied — makes each
    // row self-describing without needing to replay history to audit it.
    public long BalanceAfterKobo { get; set; }

    // Set when this entry is one leg of a Transfer; null for a plain inbound Credit.
    public Guid? RelatedTransferId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}