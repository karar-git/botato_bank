using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CoreBank.Infrastructure.Data;
using CoreBank.Domain.Enums;
using CoreBank.Domain.Exceptions;
using CoreBank.Application.DTOs.Requests;
using CoreBank.Application.DTOs.Responses;
using CoreBank.Application.Interfaces;

namespace CoreBank.Infrastructure.Services;

public class TransactionService : ITransactionService
{
    private readonly BankDbContext _db;
    private readonly IAccountService _accountService;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(BankDbContext db, IAccountService accountService, ILogger<TransactionService> logger)
    {
        _db = db;
        _accountService = accountService;
        _logger = logger;
    }

    public async Task<PaginatedResponse<LedgerEntryResponse>> GetTransactionHistoryAsync(
        Guid userId, Guid accountId, TransactionHistoryRequest request)
    {
        // Validate ownership
        await _accountService.ValidateAccountOwnership(userId, accountId);

        // Clamp pagination parameters
        request.Page = Math.Max(1, request.Page);
        request.PageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = _db.LedgerEntries
            .Where(l => l.AccountId == accountId)
            .AsQueryable();

        // Apply date filters
        if (request.FromDate.HasValue)
            query = query.Where(l => l.CreatedAt >= request.FromDate.Value);
        if (request.ToDate.HasValue)
            query = query.Where(l => l.CreatedAt <= request.ToDate.Value);

        // Apply type filter
        if (!string.IsNullOrWhiteSpace(request.Type) && Enum.TryParse<TransactionType>(request.Type, true, out var txType))
            query = query.Where(l => l.Type == txType);

        var totalCount = await query.CountAsync();

        var entries = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return new PaginatedResponse<LedgerEntryResponse>
        {
            Items = entries.Select(e => new LedgerEntryResponse
            {
                Id = e.Id,
                AccountId = e.AccountId,
                Amount = e.Amount,
                Type = e.Type.ToString(),
                Status = e.Status.ToString(),
                BalanceAfter = e.BalanceAfter,
                TransferId = e.TransferId,
                Description = e.Description,
                CreatedAt = e.CreatedAt
            }).ToList(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<byte[]> ExportToCsvAsync(Guid userId, Guid accountId, DateTime? from, DateTime? to)
    {
        await _accountService.ValidateAccountOwnership(userId, accountId);

        var entries = await GetEntriesForExport(accountId, from, to);
        var account = await _db.Accounts.FindAsync(accountId);

        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms);

        // CSV header
        await writer.WriteLineAsync("TransactionId,Date,Type,Amount,BalanceAfter,Description,TransferId");

        foreach (var entry in entries)
        {
            var line = string.Join(",",
                entry.Id,
                entry.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                entry.Type,
                entry.Amount.ToString("F2"),
                entry.BalanceAfter.ToString("F2"),
                $"\"{entry.Description.Replace("\"", "\"\"")}\"",
                entry.TransferId?.ToString() ?? "");

            await writer.WriteLineAsync(line);
        }

        await writer.FlushAsync();

        _logger.LogInformation("CSV export for account {AccountId}: {Count} entries",
            accountId, entries.Count);

        return ms.ToArray();
    }

    public async Task<byte[]> ExportToXlsxAsync(Guid userId, Guid accountId, DateTime? from, DateTime? to)
    {
        await _accountService.ValidateAccountOwnership(userId, accountId);

        var entries = await GetEntriesForExport(accountId, from, to);
        var account = await _db.Accounts.FindAsync(accountId);

        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var sheet = workbook.Worksheets.Add("Transactions");

        // Header row
        sheet.Cell(1, 1).Value = "Transaction ID";
        sheet.Cell(1, 2).Value = "Date";
        sheet.Cell(1, 3).Value = "Type";
        sheet.Cell(1, 4).Value = "Amount";
        sheet.Cell(1, 5).Value = "Balance After";
        sheet.Cell(1, 6).Value = "Description";
        sheet.Cell(1, 7).Value = "Transfer ID";

        // Style header
        var headerRange = sheet.Range(1, 1, 1, 7);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;

        // Data rows
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var row = i + 2;
            sheet.Cell(row, 1).Value = entry.Id.ToString();
            sheet.Cell(row, 2).Value = entry.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
            sheet.Cell(row, 3).Value = entry.Type.ToString();
            sheet.Cell(row, 4).Value = (double)entry.Amount;
            sheet.Cell(row, 5).Value = (double)entry.BalanceAfter;
            sheet.Cell(row, 6).Value = entry.Description;
            sheet.Cell(row, 7).Value = entry.TransferId?.ToString() ?? "";
        }

        // Summary section
        var summaryRow = entries.Count + 3;
        sheet.Cell(summaryRow, 1).Value = "Account:";
        sheet.Cell(summaryRow, 2).Value = account?.AccountNumber ?? "";
        sheet.Cell(summaryRow + 1, 1).Value = "Total Entries:";
        sheet.Cell(summaryRow + 1, 2).Value = entries.Count;
        sheet.Cell(summaryRow + 2, 1).Value = "Export Date:";
        sheet.Cell(summaryRow + 2, 2).Value = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");

        sheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);

        _logger.LogInformation("XLSX export for account {AccountId}: {Count} entries",
            accountId, entries.Count);

        return ms.ToArray();
    }

    private async Task<List<Domain.Entities.LedgerEntry>> GetEntriesForExport(
        Guid accountId, DateTime? from, DateTime? to)
    {
        var query = _db.LedgerEntries
            .Where(l => l.AccountId == accountId)
            .AsQueryable();

        if (from.HasValue)
            query = query.Where(l => l.CreatedAt >= from.Value);
        if (to.HasValue)
            query = query.Where(l => l.CreatedAt <= to.Value);

        return await query
            .OrderBy(l => l.CreatedAt)
            .ToListAsync();
    }
}
