using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CoreBank.Application.DTOs.Requests;
using CoreBank.Application.Interfaces;

namespace CoreBank.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransfersController : ControllerBase
{
    private readonly IBankingEngine _bankingEngine;

    public TransfersController(IBankingEngine bankingEngine)
    {
        _bankingEngine = bankingEngine;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

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
        var result = await _bankingEngine.TransferAsync(GetUserId(), request);
        return Ok(result);
    }
}
