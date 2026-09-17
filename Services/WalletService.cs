using NovaWallet.Data;
using NovaWallet.Dtos.Wallet;
using NovaWallet.Exceptions;
using NovaWallet.Interfaces;
using NovaWallet.Models;

namespace NovaWallet.Services
{
    public class WalletService : IWalletService
    {
        private readonly NovaWalletDbContext _context;
        private readonly IWalletRepository _walletRepository;
        private readonly IAuditLogRepository _auditLogRepository;

        public WalletService(
            NovaWalletDbContext context,
            IWalletRepository walletRepository,
            IAuditLogRepository auditLogRepository)
        {
            _context = context;
            _walletRepository = walletRepository;
            _auditLogRepository = auditLogRepository;
        }

        public async Task<WalletResponseDto> CreateWalletAsync(CreateWalletRequestDto request)
        {
            var wallet = await _walletRepository.CreateAsync(request.CustomerId, request.Currency);

            _auditLogRepository.Add(AuditLogEntry.Record(
                entityType: "Wallet",
                entityId: wallet.Id,
                action: "WalletCreated",
                oldValue: null,
                newValue: "0"));

            await _context.SaveChangesAsync();

            return new WalletResponseDto
            {
                Id = wallet.Id,
                CustomerId = wallet.CustomerId,
                Currency = wallet.Currency,
                CreatedAt = wallet.CreatedAt
            };
        }

        public async Task<BalanceResponseDto> GetBalanceAsync(Guid walletId)
        {
            var wallet = await _walletRepository.GetByIdAsync(walletId)
                ?? throw new WalletNotFoundException(walletId);

            return new BalanceResponseDto
            {
                WalletId = wallet.Id,
                BalanceKobo = wallet.BalanceKobo,
                Currency = wallet.Currency
            };
        }

        public async Task<BalanceResponseDto> CreditWalletAsync(Guid walletId, CreditWalletRequestDto request)
        {
            if (request.AmountKobo <= 0)
                throw new InvalidTransferException("AmountKobo must be positive.");

            // A credit is a single-wallet read-modify-write (BalanceKobo += amount).
            // Without a row lock, two concurrent credits could both read the same
            // starting balance and one update would silently overwrite the other
            // (a classic lost update) — so this still needs the same FOR UPDATE
            // locking as a transfer, just against one wallet instead of two.
            await using var dbTransaction = await _context.Database.BeginTransactionAsync();

            var wallets = await _walletRepository.GetForUpdateAsync(new[] { walletId });
            var wallet = wallets.FirstOrDefault() ?? throw new WalletNotFoundException(walletId);

            var balanceBefore = wallet.BalanceKobo;
            wallet.BalanceKobo += request.AmountKobo;

            _walletRepository.AddTransaction(new WalletTransaction
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                Type = TransactionType.Credit,
                AmountKobo = request.AmountKobo,
                Currency = wallet.Currency,
                BalanceAfterKobo = wallet.BalanceKobo,
                RelatedTransferId = null,
                CreatedAt = DateTimeOffset.UtcNow
            });

            _auditLogRepository.Add(AuditLogEntry.Record(
                entityType: "Wallet",
                entityId: wallet.Id,
                action: "Credit",
                oldValue: balanceBefore.ToString(),
                newValue: wallet.BalanceKobo.ToString()));

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            return new BalanceResponseDto
            {
                WalletId = wallet.Id,
                BalanceKobo = wallet.BalanceKobo,
                Currency = wallet.Currency
            };
        }

        public async Task<WalletStatementResponseDto> GetStatementAsync(Guid walletId, int page, int pageSize)
        {
            if (await _walletRepository.GetByIdAsync(walletId) is null)
                throw new WalletNotFoundException(walletId);

            page = page < 1 ? 1 : page;
            pageSize = pageSize is < 1 or > 200 ? 50 : pageSize;

            var (items, totalCount) = await _walletRepository.GetStatementAsync(walletId, page, pageSize);

            return new WalletStatementResponseDto
            {
                WalletId = walletId,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                Items = items.Select(t => new TransactionEntryDto
                {
                    Id = t.Id,
                    Type = t.Type.ToString(),
                    AmountKobo = t.AmountKobo,
                    BalanceAfterKobo = t.BalanceAfterKobo,
                    Currency = t.Currency,
                    RelatedTransferId = t.RelatedTransferId,
                    CreatedAt = t.CreatedAt
                }).ToList()
            };
        }
    }
}