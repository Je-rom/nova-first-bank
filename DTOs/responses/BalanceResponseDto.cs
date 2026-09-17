namespace NovaWallet.Dtos.Wallet
{
    public class BalanceResponseDto
    {
        public Guid WalletId { get; set; }
        public long BalanceKobo { get; set; }
        public string Currency { get; set; } = "NGN";
    }
}