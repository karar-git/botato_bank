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
    private readonly IOcrService _ocrService;

    public AuthController(IAuthService authService, IOcrService ocrService)
    {
        _authService = authService;
        _ocrService = ocrService;
    }

    /// <summary>
    /// Register a new user. Requires a photo of البطاقة الوطنية (national ID card).
    /// The ID card is processed via OCR to extract and verify the national ID number.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Register(
        [FromForm] string fullName,
        [FromForm] string email,
        [FromForm] string password,
        IFormFile idCardImage)
    {
        // Validate the image file
        if (idCardImage == null || idCardImage.Length == 0)
        {
            return BadRequest(new { code = "MISSING_ID_CARD", message = "A photo of your البطاقة الوطنية (national ID card) is required." });
        }

        // Validate file type
        var allowedTypes = new[] { "image/jpeg", "image/png", "image/jpg", "image/webp" };
        if (!allowedTypes.Contains(idCardImage.ContentType.ToLower()))
        {
            return BadRequest(new { code = "INVALID_FILE_TYPE", message = "Please upload a JPEG, PNG, or WebP image." });
        }

        // Max 10MB
        if (idCardImage.Length > 10 * 1024 * 1024)
        {
            return BadRequest(new { code = "FILE_TOO_LARGE", message = "Image must be less than 10MB." });
        }

        // Send image to external Python OCR service for validation
        using var stream = idCardImage.OpenReadStream();
        var ocrResult = await _ocrService.ValidateNationalIdAsync(stream, idCardImage.FileName);

        if (!ocrResult.IsValid)
        {
            return BadRequest(new { code = "ID_VERIFICATION_FAILED", message = ocrResult.Error });
        }

        // Build the register request with the extracted national ID number
        var request = new RegisterRequest
        {
            FullName = fullName,
            Email = email,
            Password = password,
            NationalIdNumber = ocrResult.NationalIdNumber!
        };

        var result = await _authService.RegisterAsync(request);
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

        return Ok(new { Id = userId, Email = email, Name = name });
    }
}
