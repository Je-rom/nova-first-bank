using Microsoft.EntityFrameworkCore;
using Npgsql;
using NovaWallet.Data;
using NovaWallet.Interfaces;
using NovaWallet.Models;

namespace NovaWallet.Repositories
{
    public class TransferRepository : ITransferRepository
    {
        // Postgres error code for a unique constraint violation.
        private const string UniqueViolationSqlState = "23505";

        private readonly NovaWalletDbContext _context;

        public TransferRepository(NovaWalletDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Created, Transfer Transfer)> CreatePendingOrGetExistingAsync(
            Guid fromWalletId,
            Guid toWalletId,
            long amountKobo,
            string currency,
            string idempotencyKey,
            string requestHash)
        {
            var transfer = new Transfer
            {
                Id = Guid.NewGuid(),
                FromWalletId = fromWalletId,
                ToWalletId = toWalletId,
                AmountKobo = amountKobo,
                Currency = currency,
                IdempotencyKey = idempotencyKey,
                RequestHash = requestHash,
                Status = TransferStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _context.Transfers.Add(transfer);

            try
            {
                // Saved on its own, ahead of the wallet-locking work, so a
                // duplicate request is caught (and short-circuited) before we
                // ever take row locks on the wallets involved.
                await _context.SaveChangesAsync();
                return (true, transfer);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                _context.Entry(transfer).State = EntityState.Detached;

                var existing = await _context.Transfers
                    .AsNoTracking()
                    .SingleAsync(t => t.IdempotencyKey == idempotencyKey);

                return (false, existing);
            }
        }

        public void MarkCompleted(Transfer transfer)
        {
            transfer.Status = TransferStatus.Completed;
            transfer.CompletedAt = DateTimeOffset.UtcNow;
        }

        public void MarkFailed(Transfer transfer, string reason)
        {
            transfer.Status = TransferStatus.Failed;
            transfer.FailureReason = reason;
            transfer.CompletedAt = DateTimeOffset.UtcNow;
        }

        private static bool IsUniqueViolation(DbUpdateException ex) =>
            ex.InnerException is PostgresException { SqlState: UniqueViolationSqlState };
    }
}