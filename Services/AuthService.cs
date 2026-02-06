using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using CoreBank.Data;
using CoreBank.Domain.Entities;
using CoreBank.Domain.Enums;
using CoreBank.Domain.Exceptions;
using CoreBank.DTOs.Requests;
using CoreBank.DTOs.Responses;

namespace CoreBank.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, string? idCardFrontBase64, string? idCardBackBase64);
    Task<AuthResponse> LoginAsync(LoginRequest request);
}

public class AuthService : IAuthService
{
    private readonly BankDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    public AuthService(BankDbContext db, IConfiguration config, ILogger<AuthService> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, string? idCardFrontBase64, string? idCardBackBase64)
    {
        // Check for existing user
        var exists = await _db.Users.AnyAsync(u => u.Email == request.Email.ToLower().Trim());
        if (exists)
            throw new UserAlreadyExistsException(request.Email);

        // Check for duplicate NationalIdNumber if provided
        if (!string.IsNullOrWhiteSpace(request.NationalIdNumber))
        {
            var nationalIdExists = await _db.Users.AnyAsync(u => u.NationalIdNumber == request.NationalIdNumber.Trim());
            if (nationalIdExists)
                throw new UserAlreadyExistsException($"National ID {request.NationalIdNumber} is already registered.");
        }

        // Create user (KYC pending -- employee must review ID images)
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            Email = request.Email.ToLower().Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = UserRole.Customer,
            NationalIdNumber = request.NationalIdNumber?.Trim(),
            IdCardFrontImage = idCardFrontBase64,
            IdCardBackImage = idCardBackBase64,
            KycStatus = KycStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        _logger.LogInformation("User {UserId} registered, KYC pending", user.Id);

        return new AuthResponse
        {
            Token = GenerateJwtToken(user),
            ExpiresAt = DateTime.UtcNow.AddMinutes(GetExpiryMinutes()),
            User = MapUserResponse(user)
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLower().Trim());

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new InvalidCredentialsException();

        _logger.LogInformation("User {UserId} logged in (role={Role}, kyc={KycStatus})",
            user.Id, user.Role, user.KycStatus);

        return new AuthResponse
        {
            Token = GenerateJwtToken(user),
            ExpiresAt = DateTime.UtcNow.AddMinutes(GetExpiryMinutes()),
            User = MapUserResponse(user)
        };
    }

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

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
            expires: DateTime.UtcNow.AddMinutes(GetExpiryMinutes()),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private int GetExpiryMinutes() =>
        int.TryParse(_config["Jwt:ExpiryMinutes"], out var mins) ? mins : 60;

    internal static UserResponse MapUserResponse(User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        Role = user.Role.ToString(),
        KycStatus = user.KycStatus.ToString(),
        CreatedAt = user.CreatedAt
    };
}
