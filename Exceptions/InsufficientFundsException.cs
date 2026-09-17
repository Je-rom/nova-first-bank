namespace NovaWallet.Exceptions
{
    public class InsufficientFundsException : DomainException
    {
        public override int StatusCode => 402;
        public override string ProblemType => "insufficient-funds";

        public InsufficientFundsException(Guid walletId, long requestedKobo, long availableKobo)
            : base($"Wallet '{walletId}' has insufficient funds: requested {requestedKobo} kobo, available {availableKobo} kobo.")
        {
        }
    }
}