namespace NovaWallet.Exceptions
{
    public abstract class DomainException : Exception
    {
        public abstract int StatusCode { get; }
        public abstract string ProblemType { get; }

        protected DomainException(string message) : base(message)
        {
        }
    }
}