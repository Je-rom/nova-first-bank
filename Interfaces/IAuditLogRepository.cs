using NovaWallet.Models;

namespace NovaWallet.Interfaces
{
    public interface IAuditLogRepository
    {
        void Add(AuditLogEntry entry);
    }
}