using NovaWallet.Models;
using Microsoft.EntityFrameworkCore;

namespace NovaWallet.Data
{
    public class NovaWalletDbContext : DbContext
    {
        public NovaWalletDbContext(DbContextOptions<NovaWalletDbContext> options)
            : base(options)
        {
        }

        public DbSet<Wallet> Wallets => Set<Wallet>();
        public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
        public DbSet<Transfer> Transfers => Set<Transfer>();
        public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ---------- Wallet ----------
            modelBuilder.Entity<Wallet>(entity =>
            {
                entity.HasKey(w => w.Id);

                entity.Property(w => w.CustomerId)
                    .IsRequired()
                    .HasMaxLength(64);

                entity.Property(w => w.Currency)
                    .IsRequired()
                    .HasMaxLength(3);

                entity.Property(w => w.BalanceKobo)
                    .IsRequired();

                // Speeds up "find wallet(s) for this customer" lookups.
                entity.HasIndex(w => w.CustomerId);

                entity.HasMany(w => w.Transactions)
                    .WithOne(t => t.Wallet)
                    .HasForeignKey(t => t.WalletId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ---------- WalletTransaction ----------
            modelBuilder.Entity<WalletTransaction>(entity =>
            {
                entity.HasKey(t => t.Id);

                entity.Property(t => t.Type)
                    .IsRequired()
                    .HasConversion<string>()   //store enum as readable text, not a magic int
                    .HasMaxLength(32);

                entity.Property(t => t.Currency)
                    .IsRequired()
                    .HasMaxLength(3);

                entity.Property(t => t.AmountKobo).IsRequired();
                entity.Property(t => t.BalanceAfterKobo).IsRequired();

                // Statement queries are always "by wallet, newest first" — this
                // composite index is what keeps that pagination cheap.
                entity.HasIndex(t => new { t.WalletId, t.CreatedAt });

                // Lets you fetch both legs of a transfer quickly (e.g. for audits).
                entity.HasIndex(t => t.RelatedTransferId);
            });

            // ---------- Transfer ----------
            modelBuilder.Entity<Transfer>(entity =>
            {
                entity.HasKey(t => t.Id);

                entity.Property(t => t.Currency)
                    .IsRequired()
                    .HasMaxLength(3);

                entity.Property(t => t.AmountKobo).IsRequired();

                entity.Property(t => t.IdempotencyKey)
                    .IsRequired()
                    .HasMaxLength(128);

                entity.Property(t => t.RequestHash)
                    .IsRequired()
                    .HasMaxLength(128);

                entity.Property(t => t.Status)
                    .IsRequired()
                    .HasConversion<string>()
                    .HasMaxLength(16);

                entity.Property(t => t.FailureReason)
                    .HasMaxLength(512);

                // THE critical constraint for idempotency: no two Transfer rows
                // can share a key. This is what makes concurrent replays safe —
                // only one INSERT with a given key can ever succeed.
                entity.HasIndex(t => t.IdempotencyKey).IsUnique();

                // Speeds up daily-limit calculations ("sum outbound transfers
                // for this wallet since midnight WAT").
                entity.HasIndex(t => new { t.FromWalletId, t.CreatedAt });
            });

            // ---------- AuditLogEntry ----------
            modelBuilder.Entity<AuditLogEntry>(entity =>
            {
                entity.HasKey(a => a.Id);

                entity.Property(a => a.EntityType)
                    .IsRequired()
                    .HasMaxLength(64);

                entity.Property(a => a.Action)
                    .IsRequired()
                    .HasMaxLength(64);

                entity.Property(a => a.ActorId)
                    .HasMaxLength(64);

                // Lets you pull "everything that happened to this entity" fast —
                // exactly what a judge or auditor would query.
                entity.HasIndex(a => new { a.EntityType, a.EntityId });
            });
        }
    }
}