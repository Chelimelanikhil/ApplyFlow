
using ApplyFlow.Server.DTOs;
using ApplyFlow.Server.Interfaces;
using ApplyFlow.Server.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;

namespace ApplyFlow.Server.Services;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ITokenService _tokenService;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher<User> passwordHasher,
        ITokenService tokenService)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<int?> RegisterAsync(RegisterRequest request)
    {
        var email = request.Email.Trim();

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            Role = "User",
            IsEmailVerified = false,
            IsActive = true
        };

        user.PasswordHash =
            _passwordHasher.HashPassword(user, request.Password);

        try
        {
            return await _userRepository.CreateAsync(user);
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            return null;
        }
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();

        var user = await _userRepository.GetByEmailAsync(normalizedEmail);

        if (user is null || !user.IsActive)
            return null;

        var verificationResult = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (verificationResult == PasswordVerificationResult.Failed)
            return null;

        var accessToken = _tokenService.GenerateAccessToken(
            user.UserId,
            user.FullName,
            user.Email,
            user.Role);

        var refreshToken = _tokenService.GenerateRefreshToken();

        var refreshTokenHash = _tokenService.HashToken(
            refreshToken.Token);

        await _refreshTokenRepository.SaveAsync(
            user.UserId,
            refreshTokenHash,
            refreshToken.ExpiresAt);

        return new AuthResponse
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            AccessToken = accessToken.Token,
            AccessTokenExpiresAt = accessToken.ExpiresAt,
            RefreshToken = refreshToken.Token,
            RefreshTokenExpiresAt = refreshToken.ExpiresAt
        };
    }
}
