using System.Security.Cryptography;
using System.Text;
using NovaWallet.Constants;
using NovaWallet.Data;
using NovaWallet.Dtos.Transfer;
using NovaWallet.Exceptions;
using NovaWallet.Interfaces;
using NovaWallet.Models;

namespace NovaWallet.Services
{
    public class TransferService : ITransferService
    {
        private readonly NovaWalletDbContext _context;
        private readonly IWalletRepository _walletRepository;
        private readonly ITransferRepository _transferRepository;
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly ILogger<TransferService> _logger;

        public TransferService(
            NovaWalletDbContext context,
            IWalletRepository walletRepository,
            ITransferRepository transferRepository,
            IAuditLogRepository auditLogRepository,
            ILogger<TransferService> logger)
        {
            _context = context;
            _walletRepository = walletRepository;
            _transferRepository = transferRepository;
            _auditLogRepository = auditLogRepository;
            _logger = logger;
        }

        public async Task<TransferResponseDto> ProcessTransferAsync(TransferRequestDto request, string idempotencyKey)
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey))
                throw new InvalidTransferException("Idempotency-Key header is required.");

            if (request.FromWalletId == request.ToWalletId)
                throw new InvalidTransferException("A wallet cannot transfer to itself.");

            if (request.AmountKobo <= 0)
                throw new InvalidTransferException("AmountKobo must be positive.");

            var requestHash = ComputeRequestHash(request);

            // ---- Step 1: idempotency check, BEFORE any wallet locking ----
            // Cheap and race-safe via the DB unique constraint on IdempotencyKey.
            // Catches duplicate requests before we pay the cost of locking rows.
            var (created, transfer) = await _transferRepository.CreatePendingOrGetExistingAsync(
                request.FromWalletId,
                request.ToWalletId,
                request.AmountKobo,
                currency: "NGN",
                idempotencyKey: idempotencyKey,
                requestHash: requestHash);

            if (!created)
            {
                if (transfer.RequestHash != requestHash)
                    throw new IdempotencyConflictException(idempotencyKey);

                // Same key, same payload = safe replay. Whatever the stored
                // outcome was (Completed or Failed), return it rather than
                // reprocessing. NOTE: if a prior attempt is still "Pending" this
                // means it crashed mid-flight before completing — a known gap,
                // documented in README as a place a background reconciliation
                // job would be needed in production.
                return MapToResponse(transfer);
            }

            // ---- Step 2: the locked, money-moving transaction ----
            await using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var wallets = await _walletRepository.GetForUpdateAsync(
                    new[] { request.FromWalletId, request.ToWalletId });

                var fromWallet = wallets.FirstOrDefault(w => w.Id == request.FromWalletId)
                    ?? throw new WalletNotFoundException(request.FromWalletId);

                var toWallet = wallets.FirstOrDefault(w => w.Id == request.ToWalletId)
                    ?? throw new WalletNotFoundException(request.ToWalletId);

                if (fromWallet.Currency != toWallet.Currency)
                    throw new InvalidTransferException(
                        $"Currency mismatch: {fromWallet.Currency} -> {toWallet.Currency}.");

                if (fromWallet.BalanceKobo < request.AmountKobo)
                    throw new InsufficientFundsException(fromWallet.Id, request.AmountKobo, fromWallet.BalanceKobo);

                var alreadySentToday = await _walletRepository
                    .GetOutboundTransferTotalSinceMidnightWatAsync(fromWallet.Id);

                if (alreadySentToday + request.AmountKobo > PolicyConstants.DailyOutboundLimitKobo)
                    throw new DailyLimitExceededException(
                        fromWallet.Id, alreadySentToday + request.AmountKobo, PolicyConstants.DailyOutboundLimitKobo);

                // ---- Mutate balances (plain integer kobo arithmetic only) ----
                fromWallet.BalanceKobo -= request.AmountKobo;
                toWallet.BalanceKobo += request.AmountKobo;

                // ---- Ledger entries (the statement table) ----
                _walletRepository.AddTransaction(WalletTransactionFor(
                    fromWallet.Id, TransactionType.DebitTransfer, request.AmountKobo,
                    fromWallet.Currency, fromWallet.BalanceKobo, transfer.Id));

                _walletRepository.AddTransaction(WalletTransactionFor(
                    toWallet.Id, TransactionType.CreditTransfer, request.AmountKobo,
                    toWallet.Currency, toWallet.BalanceKobo, transfer.Id));

                // ---- Audit log (separate, append-only trail) ----
                _auditLogRepository.Add(AuditEntryFor(fromWallet.Id, "DebitTransfer",
                    fromWallet.BalanceKobo + request.AmountKobo, fromWallet.BalanceKobo));

                _auditLogRepository.Add(AuditEntryFor(toWallet.Id, "CreditTransfer",
                    toWallet.BalanceKobo - request.AmountKobo, toWallet.BalanceKobo));

                transfer.Status = TransferStatus.Completed;
                transfer.CompletedAt = DateTimeOffset.UtcNow;
                _context.Transfers.Update(transfer);

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                return MapToResponse(transfer);
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();

                // The main transaction rolled back, so the wallet/ledger changes
                // above never happened — but the Transfer row itself was already
                // committed independently in Step 1. Record the failure on it in
                // a fresh, separate save so the row doesn't sit at "Pending"
                // forever and a replay of this key can see the failure outcome.
                await MarkTransferFailedIndependently(transfer.Id);
                throw;
            }
        }

        private async Task MarkTransferFailedIndependently(Guid transferId)
        {
            var freshTransfer = await _context.Transfers.FindAsync(transferId);
            if (freshTransfer is null) return;

            freshTransfer.Status = TransferStatus.Failed;
            freshTransfer.FailureReason = "Transfer validation or processing failed.";
            freshTransfer.CompletedAt = DateTimeOffset.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Don't let a bookkeeping failure here mask the original error —
                // just log it.
                _logger.LogError(ex, "Failed to mark Transfer {TransferId} as Failed", transferId);
            }
        }

        private static WalletTransaction WalletTransactionFor(
            Guid walletId, TransactionType type, long amountKobo, string currency,
            long balanceAfterKobo, Guid transferId) =>
            new()
            {
                Id = Guid.NewGuid(),
                WalletId = walletId,
                Type = type,
                AmountKobo = amountKobo,
                Currency = currency,
                BalanceAfterKobo = balanceAfterKobo,
                RelatedTransferId = transferId,
                CreatedAt = DateTimeOffset.UtcNow
            };

        private static AuditLogEntry AuditEntryFor(
            Guid walletId, string action, long oldBalanceKobo, long newBalanceKobo) =>
            new()
            {
                Id = Guid.NewGuid(),
                EntityType = "Wallet",
                EntityId = walletId,
                Action = action,
                OldValue = oldBalanceKobo.ToString(),
                NewValue = newBalanceKobo.ToString(),
                Timestamp = DateTimeOffset.UtcNow
            };

        private static string ComputeRequestHash(TransferRequestDto request)
        {
            var payload = $"{request.FromWalletId}:{request.ToWalletId}:{request.AmountKobo}";
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
            return Convert.ToHexString(bytes);
        }

        private static TransferResponseDto MapToResponse(Transfer transfer) => new()
        {
            TransferId = transfer.Id,
            FromWalletId = transfer.FromWalletId,
            ToWalletId = transfer.ToWalletId,
            AmountKobo = transfer.AmountKobo,
            Currency = transfer.Currency,
            Status = transfer.Status.ToString(),
            CreatedAt = transfer.CreatedAt,
            CompletedAt = transfer.CompletedAt
        };
    }
}