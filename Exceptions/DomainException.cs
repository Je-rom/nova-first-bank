namespace NovaWallet.Exceptions
{
    public abstract class DomainException : Exception
    {
        public abstract int StatusCode { get; }
        public abstract string ProblemType { get; } // used as the RFC 7807 "type" URI slug

        protected DomainException(string message) : base(message)
        {
        }
    }
}