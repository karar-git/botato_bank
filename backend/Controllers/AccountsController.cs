using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CoreBank.DTOs.Requests;
using CoreBank.Services;

namespace CoreBank.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Customer,Agent,Admin")]
public class AccountsController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountsController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool IsApproved() => User.FindFirstValue("IsApproved") == "True";

    /// <summary>
    /// Create a new account (checking or savings).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request)
    {
        if (!IsApproved()) return StatusCode(403, new { message = "Your account is pending approval." });
        var result = await _accountService.CreateAccountAsync(GetUserId(), request);
        return CreatedAtAction(nameof(GetAccount), new { accountId = result.Id }, result);
    }

    /// <summary>
    /// Get all accounts for the authenticated user.
    /// Balances reflect the CachedBalance (which is always consistent with the ledger).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAccounts()
    {
        if (!IsApproved()) return StatusCode(403, new { message = "Your account is pending approval." });
        var accounts = await _accountService.GetUserAccountsAsync(GetUserId());
        return Ok(accounts);
    }

    /// <summary>
    /// Get a specific account by ID.
    /// </summary>
    [HttpGet("{accountId:guid}")]
    public async Task<IActionResult> GetAccount(Guid accountId)
    {
        var account = await _accountService.GetAccountAsync(GetUserId(), accountId);
        return Ok(account);
    }

    /// <summary>
    /// Reconcile an account's cached balance against the ledger.
    /// Used for auditing - verifies that CachedBalance == SUM(LedgerEntries).
    /// </summary>
    [HttpGet("{accountId:guid}/reconcile")]
    public async Task<IActionResult> ReconcileBalance(Guid accountId)
    {
        var result = await _accountService.ReconcileBalanceAsync(GetUserId(), accountId);
        return Ok(result);
    }
}
