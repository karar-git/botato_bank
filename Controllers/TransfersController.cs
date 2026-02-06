using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CoreBank.DTOs.Requests;
using CoreBank.Services;

namespace CoreBank.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Customer,Merchant")]
public class TransfersController : ControllerBase
{
    private readonly IBankingEngine _bankingEngine;

    public TransfersController(IBankingEngine bankingEngine)
    {
        _bankingEngine = bankingEngine;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool IsKycVerified() => User.FindFirstValue("KycStatus") == "Verified";

    /// <summary>
    /// Execute an atomic fund transfer between two accounts.
    /// 
    /// Requires an idempotency key. If a retry sends the same key,
    /// the original result is returned without re-execution.
    /// 
    /// The caller must own the source account.
    /// The destination can be any active account in the system.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Transfer([FromBody] TransferRequest request)
    {
        if (!IsKycVerified()) return StatusCode(403, new { message = "Your KYC is not verified yet." });
        var result = await _bankingEngine.TransferAsync(GetUserId(), request);
        return Ok(result);
    }
}
