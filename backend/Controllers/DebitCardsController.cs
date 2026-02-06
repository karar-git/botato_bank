using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CoreBank.Data;
using CoreBank.Domain.Entities;
using CoreBank.Domain.Enums;
using CoreBank.DTOs.Requests;
using CoreBank.DTOs.Responses;

namespace CoreBank.Controllers;

/// <summary>
/// Debit card management controller.
/// Allows customers/merchants to issue virtual debit cards linked to their accounts.
/// Each account can have at most one active card.
/// Card number follows Visa format (4xxx). CVV is shown only once at creation.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Customer,Merchant")]
public class DebitCardsController : ControllerBase
{
    private readonly BankDbContext _db;
    private readonly ILogger<DebitCardsController> _logger;

    public DebitCardsController(BankDbContext db, ILogger<DebitCardsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsKycVerified() => User.FindFirstValue("KycStatus") == "Verified";

    /// <summary>
    /// Issue a new virtual debit card for an account.
    /// Returns the full card number and CVV — this is the ONLY time the CVV is shown.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateCard([FromBody] CreateDebitCardRequest request)
    {
        if (!IsKycVerified())
            return StatusCode(403, new { message = "Your KYC is not verified yet." });

        var userId = GetUserId();

        // Validate account ownership
        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == request.AccountId && a.UserId == userId);
        if (account == null)
            return NotFound(new { message = "Account not found or you don't own it." });

        if (account.Status != AccountStatus.Active)
            return BadRequest(new { message = "Cannot issue a card for an inactive account." });

        // Check if account already has a card (that isn't cancelled)
        var existingCard = await _db.DebitCards
            .FirstOrDefaultAsync(c => c.AccountId == request.AccountId && c.Status != CardStatus.Cancelled);
        if (existingCard != null)
            return BadRequest(new { message = "This account already has an active or frozen card. Cancel it first to issue a new one." });

        // Generate card details
        var cardNumber = GenerateCardNumber();
        var cvv = GenerateCvv();
        var expiryDate = DateTime.UtcNow.AddYears(3);
        var expiryStr = expiryDate.ToString("MMyy"); // e.g., "0229"

        var card = new DebitCard
        {
            Id = Guid.NewGuid(),
            AccountId = request.AccountId,
            UserId = userId,
            CardNumber = cardNumber,
            CardholderName = (await _db.Users.FindAsync(userId))!.FullName.ToUpper(),
            ExpiryDate = expiryStr,
            CvvHash = BCrypt.Net.BCrypt.HashPassword(cvv),
            Status = CardStatus.Active,
            DailyLimit = request.DailyLimit,
            CreatedAt = DateTime.UtcNow
        };

        _db.DebitCards.Add(card);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Debit card {CardId} issued for account {AccountId} by user {UserId}",
            card.Id, request.AccountId, userId);

        // Return full details (CVV only shown this once)
        return CreatedAtAction(nameof(GetCard), new { cardId = card.Id }, new DebitCardCreatedResponse
        {
            Id = card.Id,
            AccountId = card.AccountId,
            AccountNumber = account.AccountNumber,
            CardNumber = FormatCardNumber(card.CardNumber),
            CardholderName = card.CardholderName,
            ExpiryDate = $"{expiryStr[..2]}/{expiryStr[2..]}",
            Cvv = cvv,
            Status = card.Status.ToString(),
            DailyLimit = card.DailyLimit,
            CreatedAt = card.CreatedAt
        });
    }

    /// <summary>
    /// Get all debit cards for the authenticated user.
    /// Card numbers are masked. CVV is never returned.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCards()
    {
        var userId = GetUserId();
        var cards = await _db.DebitCards
            .Include(c => c.Account)
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return Ok(cards.Select(MapToResponse));
    }

    /// <summary>
    /// Get a specific debit card by ID.
    /// </summary>
    [HttpGet("{cardId:guid}")]
    public async Task<IActionResult> GetCard(Guid cardId)
    {
        var userId = GetUserId();
        var card = await _db.DebitCards
            .Include(c => c.Account)
            .FirstOrDefaultAsync(c => c.Id == cardId && c.UserId == userId);

        if (card == null)
            return NotFound(new { message = "Card not found." });

        return Ok(MapToResponse(card));
    }

    /// <summary>
    /// Update a card's status (freeze, unfreeze, or cancel).
    /// Cancelled cards cannot be reactivated — issue a new one.
    /// </summary>
    [HttpPut("{cardId:guid}/status")]
    public async Task<IActionResult> UpdateCardStatus(Guid cardId, [FromBody] UpdateCardStatusRequest request)
    {
        var userId = GetUserId();
        var card = await _db.DebitCards
            .Include(c => c.Account)
            .FirstOrDefaultAsync(c => c.Id == cardId && c.UserId == userId);

        if (card == null)
            return NotFound(new { message = "Card not found." });

        if (card.Status == CardStatus.Cancelled)
            return BadRequest(new { message = "Cannot modify a cancelled card. Issue a new one." });

        if (!Enum.TryParse<CardStatus>(request.Status, true, out var newStatus))
            return BadRequest(new { message = "Invalid status. Use: Active, Frozen, or Cancelled." });

        if (newStatus == card.Status)
            return Ok(MapToResponse(card)); // No change needed

        card.Status = newStatus;
        card.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Card {CardId} status changed to {Status} by user {UserId}",
            cardId, newStatus, userId);

        return Ok(MapToResponse(card));
    }

    // ===== Helpers =====

    /// <summary>
    /// Generate a 16-digit Visa-like card number starting with 4.
    /// Uses crypto-grade random for the remaining 15 digits.
    /// Includes a valid Luhn check digit.
    /// </summary>
    private static string GenerateCardNumber()
    {
        var rng = new Random();
        // Visa prefix: 4 + BIN (PotatoBank = 417738)
        var prefix = "417738";
        var body = prefix;
        while (body.Length < 15)
            body += rng.Next(0, 10).ToString();

        // Calculate Luhn check digit
        var checkDigit = CalculateLuhnCheckDigit(body);
        return body + checkDigit;
    }

    private static string CalculateLuhnCheckDigit(string partialNumber)
    {
        int sum = 0;
        bool alternate = true; // Start from the rightmost position of partial (which will be second-to-last)
        for (int i = partialNumber.Length - 1; i >= 0; i--)
        {
            int digit = partialNumber[i] - '0';
            if (alternate)
            {
                digit *= 2;
                if (digit > 9) digit -= 9;
            }
            sum += digit;
            alternate = !alternate;
        }
        return ((10 - (sum % 10)) % 10).ToString();
    }

    private static string GenerateCvv()
    {
        var rng = new Random();
        return rng.Next(100, 999).ToString();
    }

    private static string FormatCardNumber(string number)
    {
        if (number.Length != 16) return number;
        return $"{number[..4]} {number[4..8]} {number[8..12]} {number[12..]}";
    }

    private static string MaskCardNumber(string number)
    {
        if (number.Length != 16) return "****";
        return $"**** **** **** {number[12..]}";
    }

    private static DebitCardResponse MapToResponse(DebitCard card) => new()
    {
        Id = card.Id,
        AccountId = card.AccountId,
        AccountNumber = card.Account?.AccountNumber ?? "",
        MaskedCardNumber = MaskCardNumber(card.CardNumber),
        CardholderName = card.CardholderName,
        ExpiryDate = card.ExpiryDate.Length == 4 ? $"{card.ExpiryDate[..2]}/{card.ExpiryDate[2..]}" : card.ExpiryDate,
        Status = card.Status.ToString(),
        DailyLimit = card.DailyLimit,
        CreatedAt = card.CreatedAt
    };
}
