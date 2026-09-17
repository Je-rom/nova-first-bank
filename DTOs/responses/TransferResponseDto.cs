namespace NovaWallet.Dtos.Transfer
{
    public class TransferResponseDto
    {
        public Guid TransferId { get; set; }
        public Guid FromWalletId { get; set; }
        public Guid ToWalletId { get; set; }
        public long AmountKobo { get; set; }
        public string Currency { get; set; } = "NGN";
        public string Status { get; set; } = default!;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
    }
}