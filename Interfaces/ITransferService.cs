using NovaWallet.Dtos.Transfer;

namespace NovaWallet.Interfaces
{
    public interface ITransferService
    {
        /// <summary>
        /// Processes a wallet-to-wallet transfer. idempotencyKey comes from the
        /// caller's Idempotency-Key header. Safe to call concurrently, including
        /// with the exact same (request, idempotencyKey) pair — replays return the
        /// original result rather than moving money twice.
        /// </summary>
        Task<TransferResponseDto> ProcessTransferAsync(TransferRequestDto request, string idempotencyKey);
    }
}