using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CoreBank.Data;

namespace CoreBank.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Customer,Agent,Admin")]
public class ChatController : ControllerBase
{
    private readonly BankDbContext _db;
    private readonly HttpClient _httpClient;
    private readonly string _chatApiUrl;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        BankDbContext db,
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<ChatController> logger)
    {
        _db = db;
        _httpClient = httpClientFactory.CreateClient();
        _chatApiUrl = config["Ocr:ApiUrl"]?.Replace("/validate-id", "/chat")
            ?? "http://localhost:8000/chat";
        _logger = logger;
    }

    /// <summary>
    /// Chat with the AI banking assistant. The backend automatically
    /// attaches the user's account and transaction data as context.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var userName = User.FindFirstValue(ClaimTypes.Name) ?? "Customer";

        // Gather user's accounts
        var accounts = await _db.Accounts
            .Where(a => a.UserId == userId)
            .Select(a => new
            {
                accountNumber = a.AccountNumber,
                type = a.Type.ToString(),
                balance = a.CachedBalance,
                currency = a.Currency,
                status = a.Status.ToString()
            })
            .ToListAsync();

        // Gather recent transactions (last 20 across all accounts)
        var accountIds = await _db.Accounts
            .Where(a => a.UserId == userId)
            .Select(a => a.Id)
            .ToListAsync();

        var recentTransactions = await _db.LedgerEntries
            .Where(l => accountIds.Contains(l.AccountId))
            .OrderByDescending(l => l.CreatedAt)
            .Take(20)
            .Select(l => new
            {
                date = l.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                type = l.Type.ToString(),
                amount = l.Amount,
                description = l.Description ?? "",
                balanceAfter = l.BalanceAfter
            })
            .ToListAsync();

        // Build the payload for the Python AI service
        var payload = new
        {
            message = request.Message,
            conversationHistory = request.ConversationHistory ?? new List<ChatMessage>(),
            userContext = new
            {
                userName,
                accounts,
                recentTransactions
            }
        };

        try
        {
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_chatApiUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Chat API returned {StatusCode}: {Body}", response.StatusCode, responseBody);
                return StatusCode(502, new { message = "Chat service is temporarily unavailable." });
            }

            var result = JsonSerializer.Deserialize<JsonElement>(responseBody);
            return Ok(new { reply = result.GetProperty("reply").GetString() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call chat API");
            return StatusCode(502, new { message = "Chat service is temporarily unavailable." });
        }
    }
}

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public List<ChatMessage>? ConversationHistory { get; set; }
}

public class ChatMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
