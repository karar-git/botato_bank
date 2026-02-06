using CoreBank.Application.DTOs.Requests;
using CoreBank.Application.DTOs.Responses;

namespace CoreBank.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
}
