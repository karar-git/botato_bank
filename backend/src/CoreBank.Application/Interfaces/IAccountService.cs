using CoreBank.Application.DTOs.Requests;
using CoreBank.Application.DTOs.Responses;

namespace CoreBank.Application.Interfaces;

public interface IAccountService
{
    Task<AccountResponse> CreateAccountAsync(Guid userId, CreateAccountRequest request);
    Task<List<AccountResponse>> GetUserAccountsAsync(Guid userId);
    Task<AccountResponse> GetAccountAsync(Guid userId, Guid accountId);
    Task<decimal> GetLedgerBalanceAsync(Guid accountId);
    Task<BalanceReconciliationResponse> ReconcileBalanceAsync(Guid userId, Guid accountId);
    Task ValidateAccountOwnership(Guid userId, Guid accountId);
}
