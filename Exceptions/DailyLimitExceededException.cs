namespace NovaWallet.Exceptions
{
    public class DailyLimitExceededException : DomainException
    {
        public override int StatusCode => 422;
        public override string ProblemType => "daily-limit-exceeded";

        public DailyLimitExceededException(Guid walletId, long attemptedTotalKobo, long limitKobo)
            : base($"Wallet '{walletId}' would exceed its daily outbound limit: attempted total {attemptedTotalKobo} kobo, limit {limitKobo} kobo.")
        {
        }
    }
}