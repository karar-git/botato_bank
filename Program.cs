using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using CoreBank.Data;
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
var jwtKey = builder.Configuration["Jwt:Key"] ?? "DEFAULT-DEV-KEY-CHANGE-IN-PRODUCTION-MIN-32-BYTES!!";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "CoreBank",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "CoreBankClients",
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero // No tolerance for expired tokens
        };
    });

builder.Services.AddAuthorization();

// ===== SERVICES (DI) =====
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IBankingEngine, BankingEngine>();
builder.Services.AddScoped<ITransactionService, TransactionService>();

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

// ===== HEALTH CHECK ENDPOINT =====
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// ===== AUTO-MIGRATE DATABASE =====
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<BankDbContext>();
    db.Database.EnsureCreated();
    Console.WriteLine("Database initialized successfully.");
}
catch (Exception ex)
{
    Console.WriteLine($"WARNING: Database initialization failed: {ex.Message}");
    Console.WriteLine("The application will start but database operations may fail.");
}

// Railway sets PORT env var — bind to 0.0.0.0 so the container is reachable
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Urls.Clear();
app.Urls.Add($"http://0.0.0.0:{port}");

Console.WriteLine($"Starting on port {port}...");
app.Run();
