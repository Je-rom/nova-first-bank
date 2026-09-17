using NovaWallet.Models;

namespace NovaWallet.Interfaces
{
    public interface IAuditLogRepository
    {
        // Insert-only, deliberately — there is no Update/Delete method here.
        void Add(AuditLogEntry entry);
    }
}