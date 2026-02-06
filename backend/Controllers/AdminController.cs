using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CoreBank.Data;
using CoreBank.Domain.Enums;
using CoreBank.Domain.Entities;
using CoreBank.DTOs.Requests;
using CoreBank.DTOs.Responses;

namespace CoreBank.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly BankDbContext _db;
    private readonly ILogger<AdminController> _logger;

    public AdminController(BankDbContext db, ILogger<AdminController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// List all users pending approval (have uploaded ID images but not yet approved).
    /// </summary>
    [HttpGet("pending-users")]
    public async Task<IActionResult> GetPendingUsers()
    {
        var users = await _db.Users
            .Where(u => !u.IsApproved && u.IdCardFrontImage != null)
            .OrderBy(u => u.CreatedAt)
            .Select(u => new AdminUserResponse
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Role = u.Role.ToString(),
                IsApproved = u.IsApproved,
                RejectionReason = u.RejectionReason,
                IdCardFrontImage = u.IdCardFrontImage,
                IdCardBackImage = u.IdCardBackImage,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return Ok(users);
    }

    /// <summary>
    /// List all users in the system.
    /// </summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _db.Users
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new AdminUserResponse
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Role = u.Role.ToString(),
                IsApproved = u.IsApproved,
                RejectionReason = u.RejectionReason,
                // Don't send images in list view for performance
                IdCardFrontImage = null,
                IdCardBackImage = null,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return Ok(users);
    }

    /// <summary>
    /// Get a specific user's details including ID card images for review.
    /// </summary>
    [HttpGet("users/{userId}")]
    public async Task<IActionResult> GetUser(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found." });

        return Ok(new AdminUserResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.ToString(),
            IsApproved = user.IsApproved,
            RejectionReason = user.RejectionReason,
            IdCardFrontImage = user.IdCardFrontImage,
            IdCardBackImage = user.IdCardBackImage,
            CreatedAt = user.CreatedAt
        });
    }

    /// <summary>
    /// Approve or reject a user after reviewing their ID card images.
    /// When approved, a checking account is auto-created for the user.
    /// </summary>
    [HttpPost("users/{userId}/approve")]
    public async Task<IActionResult> ApproveUser(Guid userId, [FromBody] ApproveUserRequest request)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found." });

        if (user.IsApproved)
            return BadRequest(new { message = "User is already approved." });

        if (request.Approved)
        {
            user.IsApproved = true;
            user.RejectionReason = null;
            user.UpdatedAt = DateTime.UtcNow;

            // Auto-create checking account on approval
            var account = new Account
            {
                Id = Guid.NewGuid(),
                AccountNumber = GenerateAccountNumber("CHK"),
                UserId = user.Id,
                Type = AccountType.Checking,
                Status = AccountStatus.Active,
                CachedBalance = 0m,
                Currency = "USD",
                CreatedAt = DateTime.UtcNow,
                RowVersion = 0
            };
            _db.Accounts.Add(account);

            _logger.LogInformation("Admin approved user {UserId}, created account {AccountNumber}",
                userId, account.AccountNumber);
        }
        else
        {
            user.RejectionReason = request.RejectionReason ?? "ID verification failed.";
            user.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation("Admin rejected user {UserId}: {Reason}",
                userId, user.RejectionReason);
        }

        await _db.SaveChangesAsync();

        return Ok(new { message = request.Approved ? "User approved." : "User rejected.", userId });
    }

    /// <summary>
    /// Change a user's role.
    /// </summary>
    [HttpPost("users/{userId}/role")]
    public async Task<IActionResult> SetUserRole(Guid userId, [FromBody] SetUserRoleRequest request)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found." });

        if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
            return BadRequest(new { message = $"Invalid role. Valid roles: {string.Join(", ", Enum.GetNames<UserRole>())}" });

        user.Role = role;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Admin changed user {UserId} role to {Role}", userId, role);

        return Ok(new { message = $"User role updated to {role}.", userId });
    }

    private static string GenerateAccountNumber(string prefix)
    {
        var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
        var randomPart = Guid.NewGuid().ToString("N")[..6].ToUpper();
        return $"{prefix}-{datePart}-{randomPart}";
    }
}
