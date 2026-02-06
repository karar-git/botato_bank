using CoreBank.Application.DTOs.Requests;
using CoreBank.Application.DTOs.Responses;

namespace CoreBank.Application.Interfaces;

public interface ITransactionService
{
    Task<PaginatedResponse<LedgerEntryResponse>> GetTransactionHistoryAsync(
        Guid userId, Guid accountId, TransactionHistoryRequest request);
    Task<byte[]> ExportToCsvAsync(Guid userId, Guid accountId, DateTime? from, DateTime? to);
    Task<byte[]> ExportToXlsxAsync(Guid userId, Guid accountId, DateTime? from, DateTime? to);
}
