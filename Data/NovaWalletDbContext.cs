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

                entity.HasIndex(w => w.CustomerId);

                entity.HasMany(w => w.Transactions)
                    .WithOne(t => t.Wallet)
                    .HasForeignKey(t => t.WalletId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<WalletTransaction>(entity =>
            {
                entity.HasKey(t => t.Id);

                entity.Property(t => t.Type)
                    .IsRequired()
                    .HasConversion<string>()   
                    .HasMaxLength(32);

                entity.Property(t => t.Currency)
                    .IsRequired()
                    .HasMaxLength(3);

                entity.Property(t => t.AmountKobo).IsRequired();
                entity.Property(t => t.BalanceAfterKobo).IsRequired();

     
                entity.HasIndex(t => new { t.WalletId, t.CreatedAt });

                entity.HasIndex(t => t.RelatedTransferId);
            });

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


                entity.HasIndex(t => t.IdempotencyKey).IsUnique();


                entity.HasIndex(t => new { t.FromWalletId, t.CreatedAt });
            });

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

       
                entity.HasIndex(a => new { a.EntityType, a.EntityId });
            });
        }
    }
}