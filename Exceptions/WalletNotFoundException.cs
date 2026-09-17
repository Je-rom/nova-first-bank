namespace NovaWallet.Exceptions
{
    public class WalletNotFoundException : DomainException
    {
        public override int StatusCode => 404;
        public override string ProblemType => "wallet-not-found";

        public WalletNotFoundException(Guid walletId)
            : base($"Wallet '{walletId}' was not found.")
        {
        }
    }
}