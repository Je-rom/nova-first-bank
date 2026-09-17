using Microsoft.EntityFrameworkCore;
using NovaWallet.Data;
using NovaWallet.Interfaces;
using NovaWallet.Models;

namespace NovaWallet.Repositories
{
    public class WalletRepository : IWalletRepository
    {
        // WAT = West Africa Time, UTC+1, no DST. Use the IANA id ("Africa/Lagos")
        // rather than the Windows id since this runs in a Linux container.
        private static readonly TimeZoneInfo WatTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("Africa/Lagos");

        private readonly NovaWalletDbContext _context;

        public WalletRepository(NovaWalletDbContext context)
        {
            _context = context;
        }

        public async Task<Wallet> CreateAsync(string customerId, string currency = "NGN")
        {
            var wallet = new Wallet
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                Currency = currency,
                BalanceKobo = 0,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _context.Wallets.Add(wallet);
            await _context.SaveChangesAsync();
            return wallet;
        }

        public Task<Wallet?> GetByIdAsync(Guid walletId) =>
            _context.Wallets.AsNoTracking().FirstOrDefaultAsync(w => w.Id == walletId);

        public async Task<List<Wallet>> GetForUpdateAsync(IEnumerable<Guid> walletIds)
        {
            // THE deadlock-avoidance trick: lock wallets one at a time, in a fixed
            // ascending-Id order, no matter which direction the transfer runs.
            // A->B and a concurrent B->A both try to lock the lower Id first, so
            // neither can end up blocked waiting on the other — no deadlock possible.
            var orderedIds = walletIds.Distinct().OrderBy(id => id).ToList();

            var wallets = new List<Wallet>(orderedIds.Count);

            foreach (var id in orderedIds)
            {
                // Each of these blocks if another transaction already holds the
                // lock on this row, until that transaction commits or rolls back.
                // MUST be called inside an active transaction on _context, or the
                // lock is released immediately and provides no protection.
                var result = await _context.Wallets
                    .FromSqlInterpolated($@"SELECT * FROM ""Wallets"" WHERE ""Id"" = {id} FOR UPDATE")
                    .AsTracking()
                    .ToListAsync();

                var wallet = result.FirstOrDefault();
                if (wallet is not null)
                {
                    wallets.Add(wallet);
                }
            }

            return wallets;
        }

        public async Task<(List<WalletTransaction> Items, int TotalCount)> GetStatementAsync(
            Guid walletId, int page, int pageSize)
        {
            var query = _context.WalletTransactions
                .AsNoTracking()
                .Where(t => t.WalletId == walletId)
                .OrderByDescending(t => t.CreatedAt);

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<long> GetOutboundTransferTotalSinceMidnightWatAsync(Guid walletId)
        {
            var watNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, WatTimeZone);
            var watMidnight = new DateTimeOffset(watNow.Year, watNow.Month, watNow.Day, 0, 0, 0, watNow.Offset);
            var utcMidnight = watMidnight.ToUniversalTime();

            return await _context.WalletTransactions
                .AsNoTracking()
                .Where(t => t.WalletId == walletId
                            && t.Type == TransactionType.DebitTransfer
                            && t.CreatedAt >= utcMidnight)
                .SumAsync(t => (long?)t.AmountKobo) ?? 0L;
        }

        public void AddTransaction(WalletTransaction transaction) =>
            _context.WalletTransactions.Add(transaction);
    }
}