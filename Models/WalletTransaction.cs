namespace NovaWallet.Models;

public class WalletTransaction
{
    public Guid Id { get; set; }
    public Guid WalletId { get; set; }
    public Wallet? Wallet { get; set; }
    public TransactionType Type { get; set; }
    public long AmountKobo { get; set; }
    public string Currency { get; set; } = "NGN";
    public long BalanceAfterKobo { get; set; }
    public Guid? RelatedTransferId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}