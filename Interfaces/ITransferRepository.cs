using NovaWallet.Models;

namespace NovaWallet.Interfaces
{
    public interface ITransferRepository
    {
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