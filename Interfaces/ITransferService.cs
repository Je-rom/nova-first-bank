using NovaWallet.Dtos.Transfer;

namespace NovaWallet.Interfaces
{
    public interface ITransferService
    {
        Task<TransferResponseDto> ProcessTransferAsync(TransferRequestDto request, string idempotencyKey);
    }
}