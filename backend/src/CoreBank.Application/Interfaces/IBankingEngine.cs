using CoreBank.Application.DTOs.Requests;
using CoreBank.Application.DTOs.Responses;

namespace CoreBank.Application.Interfaces;

/// <summary>
/// CORE BANKING ENGINE
/// 
/// This is the ONLY component authorized to move money. No controller, no admin endpoint,
/// no background job may modify balances directly. All money movement flows through this engine.
/// </summary>
public interface IBankingEngine
{
    Task<LedgerEntryResponse> DepositAsync(Guid userId, Guid accountId, DepositRequest request);
    Task<LedgerEntryResponse> WithdrawAsync(Guid userId, Guid accountId, WithdrawRequest request);
    Task<TransferResponse> TransferAsync(Guid userId, TransferRequest request);
}
