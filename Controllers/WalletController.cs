using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CoreBank.DTOs.Requests;
using CoreBank.Services;

namespace CoreBank.Controllers;

[ApiController]
[Route("api/accounts/{accountId:guid}")]
[Authorize]
public class WalletController : ControllerBase
{
    private readonly IBankingEngine _bankingEngine;

    public WalletController(IBankingEngine bankingEngine)
    {
        _bankingEngine = bankingEngine;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool IsKycVerified() => User.FindFirstValue("KycStatus") == "Verified";

    /// <summary>
    /// Deposit funds into an account.
    /// Only Merchants can deposit (e.g. from bank or cash register).
    /// </summary>
    [HttpPost("deposit")]
    [Authorize(Roles = "Merchant")]
    public async Task<IActionResult> Deposit(Guid accountId, [FromBody] DepositRequest request)
    {
        if (!IsKycVerified()) return StatusCode(403, new { message = "Your KYC is not verified yet." });
        var result = await _bankingEngine.DepositAsync(GetUserId(), accountId, request);
        return Ok(result);
    }

    /// <summary>
    /// Withdraw funds from an account.
    /// Both Customers and Merchants can withdraw. ATM/branch number required.
    /// </summary>
    [HttpPost("withdraw")]
    [Authorize(Roles = "Customer,Merchant")]
    public async Task<IActionResult> Withdraw(Guid accountId, [FromBody] WithdrawRequest request)
    {
        if (!IsKycVerified()) return StatusCode(403, new { message = "Your KYC is not verified yet." });
        var result = await _bankingEngine.WithdrawAsync(GetUserId(), accountId, request);
        return Ok(result);
    }
}
