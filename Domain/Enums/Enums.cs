namespace CoreBank.Domain.Enums;

public enum UserRole
{
    Customer = 1,
    Merchant = 2,
    Employee = 3,
    Admin = 4
}

public enum KycStatus
{
    Pending = 1,
    Verified = 2,
    Rejected = 3
}

public enum AccountType
{
    Checking = 1,
    Savings = 2,
    Business = 3
}

public enum AccountStatus
{
    Active = 1,
    Frozen = 2,
    Closed = 3
}

public enum TransactionType
{
    Deposit = 1,
    Withdrawal = 2,
    TransferDebit = 3,
    TransferCredit = 4
}

public enum TransactionStatus
{
    Pending = 1,
    Completed = 2,
    Failed = 3,
    Reversed = 4
}

public enum TransferStatus
{
    Pending = 1,
    Completed = 2,
    Failed = 3
}

public enum CardStatus
{
    Active = 1,
    Frozen = 2,
    Cancelled = 3
}
