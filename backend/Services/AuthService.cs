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
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
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

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Check for existing user
        var exists = await _db.Users.AnyAsync(u => u.Email == request.Email.ToLower().Trim());
        if (exists)
            throw new UserAlreadyExistsException(request.Email);

        // Check for duplicate national ID
        var idExists = await _db.Users.AnyAsync(u => u.NationalIdNumber == request.NationalIdNumber);
        if (idExists)
            throw new InvalidOperationException("A user with this national ID number already exists.");

        // Create user
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            Email = request.Email.ToLower().Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            NationalIdNumber = request.NationalIdNumber,
            IsIdVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        // Auto-create checking account per banking requirement
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

        _db.Users.Add(user);
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();

        _logger.LogInformation("User {UserId} registered with checking account {AccountNumber}",
            user.Id, account.AccountNumber);

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

        _logger.LogInformation("User {UserId} logged in", user.Id);

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

    private static string GenerateAccountNumber(string prefix)
    {
        var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
        var randomPart = Guid.NewGuid().ToString("N")[..6].ToUpper();
        return $"{prefix}-{datePart}-{randomPart}";
    }

    private static UserResponse MapUserResponse(User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        CreatedAt = user.CreatedAt
    };
}
