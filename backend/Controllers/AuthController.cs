using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CoreBank.DTOs.Requests;
using CoreBank.Services;

namespace CoreBank.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Register a new user. Requires front and back photos of البطاقة الوطنية (national ID card).
    /// User will be pending until an admin reviews and approves the ID images.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Register(
        [FromForm] string fullName,
        [FromForm] string email,
        [FromForm] string password,
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
            Password = password
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
    /// Get current authenticated user info.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);
        var name = User.FindFirstValue(ClaimTypes.Name);
        var role = User.FindFirstValue(ClaimTypes.Role);
        var isApproved = User.FindFirstValue("IsApproved");

        return Ok(new { Id = userId, Email = email, Name = name, Role = role, IsApproved = isApproved });
    }
}
