namespace NovaWallet.Models;

public class Wallet
{
    public Guid Id { get; set; }
    public string CustomerId { get; set; } = default!;
    public long BalanceKobo { get; set; }
    public string Currency { get; set; } = "NGN";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();
}