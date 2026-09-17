namespace NovaWallet.Exceptions
{
    public class InvalidTransferException : DomainException
    {
        public override int StatusCode => 400;
        public override string ProblemType => "invalid-transfer";

        public InvalidTransferException(string message) : base(message)
        {
        }
    }
}   