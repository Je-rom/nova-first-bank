using NovaWallet.Dtos.Wallet;

namespace NovaWallet.Interfaces
{
    public interface IWalletService
    {
        Task<WalletResponseDto> CreateWalletAsync(CreateWalletRequestDto request);

        Task<BalanceResponseDto> GetBalanceAsync(Guid walletId);

        Task<BalanceResponseDto> CreditWalletAsync(Guid walletId, CreditWalletRequestDto request);

        Task<WalletStatementResponseDto> GetStatementAsync(Guid walletId, int page, int pageSize);
    }
}