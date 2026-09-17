namespace NovaWallet.Dtos.Wallet
{
    public class WalletResponseDto
    {
        public Guid Id { get; set; }
        public string CustomerId { get; set; } = default!;
        public string Currency { get; set; } = "NGN";
        public DateTimeOffset CreatedAt { get; set; }
    }
}