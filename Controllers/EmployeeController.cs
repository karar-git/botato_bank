using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CoreBank.Data;
using CoreBank.Domain.Enums;
using CoreBank.DTOs.Requests;
using CoreBank.DTOs.Responses;
using CoreBank.Services;

namespace CoreBank.Controllers;

/// <summary>
/// Employee controller - CSV bulk operations.
/// Employee cannot move money manually. Only via CSV upload which processes
/// each row through the BankingEngine (deposit/withdraw).
/// 
/// CSV format: NationalId,Amount,Operation
/// Operation must be DEPOSIT or WITHDRAW.
/// Each row is processed against the user's first active checking account.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Employee")]
public class EmployeeController : ControllerBase
{
    private readonly BankDbContext _db;
    private readonly IBankingEngine _bankingEngine;
    private readonly ILogger<EmployeeController> _logger;

    public EmployeeController(BankDbContext db, IBankingEngine bankingEngine, ILogger<EmployeeController> logger)
    {
        _db = db;
        _bankingEngine = bankingEngine;
        _logger = logger;
    }

    /// <summary>
    /// Upload a CSV file with bulk deposit/withdraw instructions.
    /// CSV columns: NationalId, Amount, Operation (DEPOSIT or WITHDRAW).
    /// Each row is processed through the BankingEngine — no bypasses.
    /// </summary>
    [HttpPost("csv-upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadCsv(IFormFile csvFile)
    {
        if (csvFile == null || csvFile.Length == 0)
            return BadRequest(new { code = "MISSING_FILE", message = "A CSV file is required." });

        if (csvFile.Length > 5 * 1024 * 1024) // 5MB max
            return BadRequest(new { code = "FILE_TOO_LARGE", message = "CSV file must be less than 5MB." });

        // Read CSV content
        var lines = new List<string>();
        using (var reader = new StreamReader(csvFile.OpenReadStream()))
        {
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (!string.IsNullOrWhiteSpace(line))
                    lines.Add(line.Trim());
            }
        }

        if (lines.Count < 2) // header + at least one data row
            return BadRequest(new { code = "EMPTY_CSV", message = "CSV file must have a header row and at least one data row." });

        // Validate header
        var header = lines[0].ToLower().Replace(" ", "");
        if (!header.Contains("nationalid") || !header.Contains("amount") || !header.Contains("operation"))
            return BadRequest(new { code = "INVALID_HEADER", message = "CSV header must contain: NationalId, Amount, Operation" });

        var results = new List<CsvRowResult>();
        var employeeId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // Process each data row
        for (int i = 1; i < lines.Count; i++)
        {
            var rowResult = new CsvRowResult { RowNumber = i };

            try
            {
                var parts = lines[i].Split(',');
                if (parts.Length < 3)
                {
                    rowResult.Success = false;
                    rowResult.Error = "Row must have 3 columns: NationalId, Amount, Operation";
                    results.Add(rowResult);
                    continue;
                }

                var nationalId = parts[0].Trim();
                var amountStr = parts[1].Trim();
                var operation = parts[2].Trim().ToUpper();

                rowResult.NationalId = nationalId;
                rowResult.Operation = operation;

                // Validate amount
                if (!decimal.TryParse(amountStr, out var amount) || amount <= 0)
                {
                    rowResult.Success = false;
                    rowResult.Error = $"Invalid amount: {amountStr}";
                    results.Add(rowResult);
                    continue;
                }
                rowResult.Amount = amount;

                // Validate operation
                if (operation != "DEPOSIT" && operation != "WITHDRAW")
                {
                    rowResult.Success = false;
                    rowResult.Error = $"Invalid operation: {operation}. Must be DEPOSIT or WITHDRAW.";
                    results.Add(rowResult);
                    continue;
                }

                // Look up user by NationalIdNumber
                var user = await _db.Users
                    .FirstOrDefaultAsync(u => u.NationalIdNumber == nationalId);

                if (user == null)
                {
                    rowResult.Success = false;
                    rowResult.Error = $"No user found with National ID: {nationalId}";
                    results.Add(rowResult);
                    continue;
                }

                // Check KYC
                if (user.KycStatus != KycStatus.Verified)
                {
                    rowResult.Success = false;
                    rowResult.Error = $"User KYC status is {user.KycStatus}. Must be Verified.";
                    results.Add(rowResult);
                    continue;
                }

                // Get the user's first active checking account
                var account = await _db.Accounts
                    .FirstOrDefaultAsync(a => a.UserId == user.Id &&
                                             a.Type == AccountType.Checking &&
                                             a.Status == AccountStatus.Active);

                if (account == null)
                {
                    rowResult.Success = false;
                    rowResult.Error = "User has no active checking account.";
                    results.Add(rowResult);
                    continue;
                }

                rowResult.AccountNumber = account.AccountNumber;

                // Execute via BankingEngine — no bypasses
                if (operation == "DEPOSIT")
                {
                    var depositReq = new DepositRequest
                    {
                        Amount = amount,
                        Description = $"CSV bulk deposit by employee",
                        IdempotencyKey = $"CSV-{csvFile.FileName}-{i}-{DateTime.UtcNow:yyyyMMddHHmmss}"
                    };
                    var result = await _bankingEngine.DepositAsync(user.Id, account.Id, depositReq);
                    rowResult.Success = true;
                    rowResult.BalanceAfter = result.BalanceAfter;
                }
                else // WITHDRAW
                {
                    var withdrawReq = new WithdrawRequest
                    {
                        Amount = amount,
                        Description = $"CSV bulk withdrawal by employee",
                        IdempotencyKey = $"CSV-{csvFile.FileName}-{i}-{DateTime.UtcNow:yyyyMMddHHmmss}"
                    };
                    var result = await _bankingEngine.WithdrawAsync(user.Id, account.Id, withdrawReq);
                    rowResult.Success = true;
                    rowResult.BalanceAfter = result.BalanceAfter;
                }
            }
            catch (Exception ex)
            {
                rowResult.Success = false;
                rowResult.Error = ex.Message;
            }

            results.Add(rowResult);
        }

        var response = new CsvUploadResponse
        {
            TotalRows = results.Count,
            SuccessCount = results.Count(r => r.Success),
            FailureCount = results.Count(r => !r.Success),
            Results = results
        };

        _logger.LogInformation(
            "Employee {EmployeeId} CSV upload: {Total} rows, {Success} succeeded, {Failed} failed",
            employeeId, response.TotalRows, response.SuccessCount, response.FailureCount);

        return Ok(response);
    }
}
