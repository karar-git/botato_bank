namespace CoreBank.Domain.Exceptions;

/// <summary>
/// Base exception for all domain-specific errors.
/// These map to specific HTTP status codes and structured error responses.
/// </summary>
public abstract class DomainException : Exception
{
    public string Code { get; }
    
    protected DomainException(string code, string message) : base(message)
    {
        Code = code;
    }
}

public class InsufficientFundsException : DomainException
{
    public Guid AccountId { get; }
    public decimal Requested { get; }
    public decimal Available { get; }

    public InsufficientFundsException(Guid accountId, decimal requested, decimal available)
        : base("INSUFFICIENT_FUNDS",
            $"Account {accountId} has insufficient funds. Requested: {requested:F2}, Available: {available:F2}")
    {
        AccountId = accountId;
        Requested = requested;
        Available = available;
    }
}

public class AccountNotFoundException : DomainException
{
    public AccountNotFoundException(Guid accountId)
        : base("ACCOUNT_NOT_FOUND", $"Account {accountId} does not exist.") { }
    
    public AccountNotFoundException(string accountNumber)
        : base("ACCOUNT_NOT_FOUND", $"Account '{accountNumber}' does not exist.") { }
}

public class AccountFrozenException : DomainException
{
    public AccountFrozenException(Guid accountId)
        : base("ACCOUNT_FROZEN", $"Account {accountId} is frozen and cannot process transactions.") { }
}

public class AccountClosedException : DomainException
{
    public AccountClosedException(Guid accountId)
        : base("ACCOUNT_CLOSED", $"Account {accountId} is closed.") { }
}

public class SelfTransferException : DomainException
{
    public SelfTransferException()
        : base("SELF_TRANSFER", "Cannot transfer funds to the same account.") { }
}

public class DuplicateOperationException : DomainException
{
    public DuplicateOperationException(string idempotencyKey)
        : base("DUPLICATE_OPERATION", $"Operation with key '{idempotencyKey}' has already been processed.") { }
}

public class InvalidAmountException : DomainException
{
    public InvalidAmountException(string reason)
        : base("INVALID_AMOUNT", reason) { }
}

public class UnauthorizedAccountAccessException : DomainException
{
    public UnauthorizedAccountAccessException()
        : base("UNAUTHORIZED_ACCESS", "You do not have access to this account.") { }
}

public class ConcurrencyConflictException : DomainException
{
    public ConcurrencyConflictException()
        : base("CONCURRENCY_CONFLICT", 
            "This operation conflicted with another concurrent operation. Please retry.") { }
}

public class UserAlreadyExistsException : DomainException
{
    public UserAlreadyExistsException(string email)
        : base("USER_EXISTS", $"A user with email '{email}' already exists.") { }
}

public class InvalidCredentialsException : DomainException
{
    public InvalidCredentialsException()
        : base("INVALID_CREDENTIALS", "Invalid email or password.") { }
}
