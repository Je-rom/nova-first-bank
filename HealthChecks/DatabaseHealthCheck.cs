using Microsoft.Extensions.Diagnostics.HealthChecks;
using NovaWallet.Data;

namespace NovaWallet.HealthChecks
{
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly NovaWalletDbContext _context;

        public DatabaseHealthCheck(NovaWalletDbContext context)
        {
            _context = context;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync(cancellationToken);

                return canConnect
                    ? HealthCheckResult.Healthy("Database connection OK.")
                    : HealthCheckResult.Unhealthy("Database is not reachable.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Database health check threw an exception.", ex);
            }
        }
    }
}