using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using CoreBank.Data;
using CoreBank.Domain.Enums;
using CoreBank.DTOs.Requests;
using CoreBank.Services;

namespace CoreBank.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly BankDbContext _db;
    private readonly IConfiguration _config;

    public AuthController(IAuthService authService, BankDbContext db, IConfiguration config)
    {
        _authService = authService;
        _db = db;
        _config = config;
    }

    /// <summary>
    /// Register a new user. Requires front and back photos of البطاقة الوطنية (national ID card).
    /// User will have KYC status Pending until an employee reviews and verifies the ID images.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Register(
        [FromForm] string fullName,
        [FromForm] string email,
        [FromForm] string password,
        [FromForm] string? nationalIdNumber,
        IFormFile idCardFront,
        IFormFile idCardBack)
    {
        // Validate front image
        if (idCardFront == null || idCardFront.Length == 0)
            return BadRequest(new { code = "MISSING_ID_FRONT", message = "A photo of the front of your البطاقة الوطنية is required." });

        // Validate back image
        if (idCardBack == null || idCardBack.Length == 0)
            return BadRequest(new { code = "MISSING_ID_BACK", message = "A photo of the back of your البطاقة الوطنية is required." });

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/jpg", "image/webp" };

        if (!allowedTypes.Contains(idCardFront.ContentType.ToLower()))
            return BadRequest(new { code = "INVALID_FILE_TYPE", message = "Front image must be JPEG, PNG, or WebP." });

        if (!allowedTypes.Contains(idCardBack.ContentType.ToLower()))
            return BadRequest(new { code = "INVALID_FILE_TYPE", message = "Back image must be JPEG, PNG, or WebP." });

        if (idCardFront.Length > 10 * 1024 * 1024)
            return BadRequest(new { code = "FILE_TOO_LARGE", message = "Front image must be less than 10MB." });

        if (idCardBack.Length > 10 * 1024 * 1024)
            return BadRequest(new { code = "FILE_TOO_LARGE", message = "Back image must be less than 10MB." });

        // Convert images to base64 for DB storage
        string frontBase64, backBase64;
        using (var ms = new MemoryStream())
        {
            await idCardFront.CopyToAsync(ms);
            frontBase64 = $"data:{idCardFront.ContentType};base64,{Convert.ToBase64String(ms.ToArray())}";
        }
        using (var ms = new MemoryStream())
        {
            await idCardBack.CopyToAsync(ms);
            backBase64 = $"data:{idCardBack.ContentType};base64,{Convert.ToBase64String(ms.ToArray())}";
        }

        var request = new RegisterRequest
        {
            FullName = fullName,
            Email = email,
            Password = password,
            NationalIdNumber = nationalIdNumber
        };

        var result = await _authService.RegisterAsync(request, frontBase64, backBase64);
        return CreatedAtAction(nameof(Register), result);
    }

    /// <summary>
    /// Login and receive a JWT token.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Get current authenticated user info (reads from DB for fresh data).
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _db.Users.FindAsync(Guid.Parse(userId!));
        if (user == null) return NotFound();

        return Ok(new
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.FullName,
            Role = user.Role.ToString(),
            KycStatus = user.KycStatus.ToString(),
            RejectionReason = user.RejectionReason
        });
    }

    /// <summary>
    /// Switch the current user's role (demo/hackathon feature).
    /// Returns a fresh JWT with the new role so the frontend can update immediately.
    /// </summary>
    [HttpPost("switch-role")]
    [Authorize]
    public async Task<IActionResult> SwitchRole([FromBody] SwitchRoleRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _db.Users.FindAsync(Guid.Parse(userId!));
        if (user == null) return NotFound();

        if (!Enum.TryParse<UserRole>(request.Role, true, out var newRole))
            return BadRequest(new { message = "Invalid role. Must be: Customer, Merchant, Employee, or Admin." });

        user.Role = newRole;
        await _db.SaveChangesAsync();

        // Generate a fresh JWT with the new role
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiryMinutes = int.TryParse(_config["Jwt:ExpiryMinutes"], out var mins) ? mins : 60;

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("KycStatus", user.KycStatus.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new
        {
            token = tokenString,
            expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes),
            user = new
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString(),
                KycStatus = user.KycStatus.ToString()
            }
        });
    }
}
