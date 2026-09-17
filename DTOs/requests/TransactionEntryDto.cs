namespace NovaWallet.Dtos.Wallet
{
    public class TransactionEntryDto
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = default!;
        public long AmountKobo { get; set; }
        public long BalanceAfterKobo { get; set; }
        public string Currency { get; set; } = "NGN";
        public Guid? RelatedTransferId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class WalletStatementResponseDto
    {
        public Guid WalletId { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public List<TransactionEntryDto> Items { get; set; } = new();
    }
}