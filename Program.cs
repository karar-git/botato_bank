using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using CoreBank.Data;
using CoreBank.Domain.Entities;
using CoreBank.Domain.Enums;
using CoreBank.Middleware;
using CoreBank.Services;

var builder = WebApplication.CreateBuilder(args);

// ===== DATABASE =====
var dbProvider = builder.Configuration["DatabaseProvider"] ?? "Sqlite";
if (dbProvider == "PostgreSQL")
{
    builder.Services.AddDbContext<BankDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
}
else
{
    // SQLite for local development / hackathon demo
    builder.Services.AddDbContext<BankDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("Sqlite")));
}

// ===== AUTHENTICATION =====
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ClockSkew = TimeSpan.Zero // No tolerance for expired tokens
        };
    });

builder.Services.AddAuthorization();

// ===== SERVICES (DI) =====
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IBankingEngine, BankingEngine>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddHttpClient(); // For ChatController to call AI service

// ===== CONTROLLERS =====
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// ===== CORS (for Svelte frontend - local + Vercel production) =====
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5173", "http://localhost:3000" };
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// ===== SWAGGER =====
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CoreBank API",
        Version = "v1",
        Description = "Production-grade core banking system with atomic transfers, " +
                      "ledger-based balance derivation, and full auditability."
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ===== MIDDLEWARE PIPELINE =====
// Order matters: exception handling wraps everything
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Swagger available in all environments for hackathon demo
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CoreBank API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ===== AUTO-MIGRATE DATABASE =====
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BankDbContext>();
    // ONE-TIME: Drop old schema and recreate with new columns (Role, IsApproved, etc.)
    // TODO: Remove EnsureDeleted() after first successful deploy
    db.Database.EnsureDeleted();
    db.Database.EnsureCreated();

    // Seed default admin user if none exists
    if (!db.Users.Any(u => u.Role == UserRole.Admin))
    {
        var admin = new User
        {
            Id = Guid.NewGuid(),
            FullName = "System Admin",
            Email = "admin@botatobank.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            Role = UserRole.Admin,
            IsApproved = true,
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(admin);
        db.SaveChanges();
        Console.WriteLine($"Seeded admin user: admin@botatobank.com / Admin@123");
    }
}

// Railway sets PORT env var — bind to 0.0.0.0 so the container is reachable
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Urls.Clear();
app.Urls.Add($"http://0.0.0.0:{port}");

app.Run();
