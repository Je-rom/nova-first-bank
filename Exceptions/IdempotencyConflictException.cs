namespace NovaWallet.Exceptions
{
    public class IdempotencyConflictException : DomainException
    {
        public override int StatusCode => 409;
        public override string ProblemType => "idempotency-key-conflict";

        public IdempotencyConflictException(string idempotencyKey)
            : base($"Idempotency key '{idempotencyKey}' was already used with a different request payload.")
        {
        }
    }
}