using NovaWallet.Data;
using NovaWallet.Interfaces;
using NovaWallet.Models;

namespace NovaWallet.Repositories
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly NovaWalletDbContext _context;

        public AuditLogRepository(NovaWalletDbContext context)
        {
            _context = context;
        }

        public void Add(AuditLogEntry entry) => _context.AuditLogEntries.Add(entry);
    }
}