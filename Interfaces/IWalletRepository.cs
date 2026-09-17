using NovaWallet.Models;

namespace NovaWallet.Interfaces
{
    public interface IWalletRepository
    {
        Task<Wallet> CreateAsync(string customerId, string currency = "NGN");

        Task<Wallet?> GetByIdAsync(Guid walletId);
        Task<List<Wallet>> GetForUpdateAsync(IEnumerable<Guid> walletIds);

        Task<(List<WalletTransaction> Items, int TotalCount)> GetStatementAsync(
            Guid walletId, int page, int pageSize);
        Task<long> GetOutboundTransferTotalSinceMidnightWatAsync(Guid walletId);

        void AddTransaction(WalletTransaction transaction);
    }
}