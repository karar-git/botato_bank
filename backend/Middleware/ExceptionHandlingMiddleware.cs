using System.Net;
using System.Text.Json;
using CoreBank.Domain.Exceptions;
using CoreBank.DTOs.Responses;

namespace CoreBank.Middleware;

/// <summary>
/// Global exception handler that maps domain exceptions to structured HTTP error responses.
/// 
/// This ensures:
/// 1. Domain exceptions never leak stack traces to clients
/// 2. Every error has a machine-readable code + human-readable message
/// 3. Unexpected errors return 500 with a safe message (no internal details)
/// 4. All errors are logged with correlation IDs for debugging
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = context.TraceIdentifier;

        var (statusCode, errorResponse) = exception switch
        {
            InsufficientFundsException e => (HttpStatusCode.BadRequest, new ErrorResponse
            {
                Code = e.Code,
                Message = e.Message,
                TraceId = traceId
            }),
            AccountNotFoundException e => (HttpStatusCode.NotFound, new ErrorResponse
            {
                Code = e.Code,
                Message = e.Message,
                TraceId = traceId
            }),
            AccountFrozenException e => (HttpStatusCode.Forbidden, new ErrorResponse
            {
                Code = e.Code,
                Message = e.Message,
                TraceId = traceId
            }),
            AccountClosedException e => (HttpStatusCode.Gone, new ErrorResponse
            {
                Code = e.Code,
                Message = e.Message,
                TraceId = traceId
            }),
            SelfTransferException e => (HttpStatusCode.BadRequest, new ErrorResponse
            {
                Code = e.Code,
                Message = e.Message,
                TraceId = traceId
            }),
            DuplicateOperationException e => (HttpStatusCode.Conflict, new ErrorResponse
            {
                Code = e.Code,
                Message = e.Message,
                TraceId = traceId
            }),
            InvalidAmountException e => (HttpStatusCode.BadRequest, new ErrorResponse
            {
                Code = e.Code,
                Message = e.Message,
                TraceId = traceId
            }),
            UnauthorizedAccountAccessException e => (HttpStatusCode.Forbidden, new ErrorResponse
            {
                Code = e.Code,
                Message = e.Message,
                TraceId = traceId
            }),
            ConcurrencyConflictException e => (HttpStatusCode.Conflict, new ErrorResponse
            {
                Code = e.Code,
                Message = e.Message,
                TraceId = traceId
            }),
            UserAlreadyExistsException e => (HttpStatusCode.Conflict, new ErrorResponse
            {
                Code = e.Code,
                Message = e.Message,
                TraceId = traceId
            }),
            InvalidCredentialsException e => (HttpStatusCode.Unauthorized, new ErrorResponse
            {
                Code = e.Code,
                Message = e.Message,
                TraceId = traceId
            }),
            // Catch-all for unknown exceptions - NEVER leak internal details
            _ => (HttpStatusCode.InternalServerError, new ErrorResponse
            {
                Code = "INTERNAL_ERROR",
                Message = "An unexpected error occurred. Please try again later.",
                TraceId = traceId
            })
        };

        // Log with different severity based on type
        if (exception is DomainException)
        {
            _logger.LogWarning("Domain error [{Code}]: {Message} (TraceId: {TraceId})",
                errorResponse.Code, exception.Message, traceId);
        }
        else
        {
            _logger.LogError(exception, "Unhandled exception (TraceId: {TraceId})", traceId);
        }

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
