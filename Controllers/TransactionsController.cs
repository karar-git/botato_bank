using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CoreBank.DTOs.Requests;
using CoreBank.Services;

namespace CoreBank.Controllers;

[ApiController]
[Route("api/accounts/{accountId:guid}/transactions")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;

    public TransactionsController(ITransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Get paginated transaction history for an account.
    /// Supports filtering by date range and transaction type.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetHistory(
        Guid accountId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? type = null)
    {
        var request = new TransactionHistoryRequest
        {
            Page = page,
            PageSize = pageSize,
            FromDate = from,
            ToDate = to,
            Type = type
        };

        var result = await _transactionService.GetTransactionHistoryAsync(GetUserId(), accountId, request);
        return Ok(result);
    }

    /// <summary>
    /// Export transaction history as CSV for auditors.
    /// </summary>
    [HttpGet("export/csv")]
    public async Task<IActionResult> ExportCsv(
        Guid accountId,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var csv = await _transactionService.ExportToCsvAsync(GetUserId(), accountId, from, to);
        return File(csv, "text/csv", $"transactions_{accountId}_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    /// <summary>
    /// Export transaction history as XLSX for auditors.
    /// </summary>
    [HttpGet("export/xlsx")]
    public async Task<IActionResult> ExportXlsx(
        Guid accountId,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var xlsx = await _transactionService.ExportToXlsxAsync(GetUserId(), accountId, from, to);
        return File(xlsx,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"transactions_{accountId}_{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }
}
