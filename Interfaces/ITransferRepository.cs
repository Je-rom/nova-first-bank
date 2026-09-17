using NovaWallet.Models;

namespace NovaWallet.Interfaces
{
    public interface ITransferRepository
    {
        /// <summary>
        /// Attempts to insert a new pending Transfer under the given idempotency
        /// key. Returns (true, transfer) if this call is the one that created the
        /// row. Returns (false, existingTransfer) if a Transfer with this key
        /// already exists — the caller compares RequestHash to decide between a
        /// safe replay (return the existing result) and a conflict (409).
        ///
        /// Race-safety comes from the unique DB constraint on IdempotencyKey, not
        /// from any locking here: if two requests with the same key race this
        /// method concurrently, the database itself only allows one INSERT to
        /// succeed. The loser is routed into the "already exists" branch.
        /// </summary>
        Task<(bool Created, Transfer Transfer)> CreatePendingOrGetExistingAsync(
            Guid fromWalletId,
            Guid toWalletId,
            long amountKobo,
            string currency,
            string idempotencyKey,
            string requestHash);

        void MarkCompleted(Transfer transfer);

        void MarkFailed(Transfer transfer, string reason);
    }
}