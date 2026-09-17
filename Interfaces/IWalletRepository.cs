using NovaWallet.Models;

namespace NovaWallet.Interfaces
{
    public interface IWalletRepository
    {
        Task<Wallet> CreateAsync(string customerId, string currency = "NGN");

        Task<Wallet?> GetByIdAsync(Guid walletId);

        /// <summary>
        /// Loads one or more wallets with a row-level lock (SELECT ... FOR UPDATE),
        /// in ascending Id order, inside the caller's active transaction.
        /// MUST be called from within an explicit transaction. Locking wallets in
        /// a fixed order (by Id) regardless of transfer direction is what prevents
        /// deadlocks between two transfers that touch the same pair of wallets in
        /// opposite directions (A->B racing B->A).
        /// </summary>
        Task<List<Wallet>> GetForUpdateAsync(IEnumerable<Guid> walletIds);

        Task<(List<WalletTransaction> Items, int TotalCount)> GetStatementAsync(
            Guid walletId, int page, int pageSize);

        /// <summary>
        /// Sum of AmountKobo for outbound transfer debits from this wallet since
        /// the start of the current day in WAT. Used to enforce the daily limit;
        /// computed live inside the same locked transaction rather than a
        /// separately-maintained counter, so there's no reset job to get wrong.
        /// </summary>
        Task<long> GetOutboundTransferTotalSinceMidnightWatAsync(Guid walletId);

        void AddTransaction(WalletTransaction transaction);
    }
}