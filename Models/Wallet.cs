namespace NovaWallet.Models;

public class Wallet
{
    public Guid Id { get; set; }
    public string CustomerId { get; set; } = default!;

    // Always an integer number of kobo. Never use decimal/double for this field
    // or anywhere it flows through — that's the whole "no float in the money
    // path" requirement from the brief.
    public long BalanceKobo { get; set; }

    public string Currency { get; set; } = "NGN";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();
}