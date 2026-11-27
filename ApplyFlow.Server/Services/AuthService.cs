
using ApplyFlow.Server.DTOs;
using ApplyFlow.Server.Interfaces;
using ApplyFlow.Server.Models;
using ApplyFlow.Server.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;

namespace ApplyFlow.Server.Services;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IEmailVerificationRepository _emailVerificationRepository;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;


    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher<User> passwordHasher,
        ITokenService tokenService,
        IEmailVerificationRepository emailVerificationRepository,
        IEmailSender emailSender,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _emailVerificationRepository = emailVerificationRepository;
        _emailSender = emailSender;
        _configuration = configuration;
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
            var userId = await _userRepository.CreateAsync(user);

            user.UserId = userId;

            await SendVerificationAsync(user);

            return userId;
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

        if (user is null || !user.IsActive || !user.IsEmailVerified)
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

    public async Task<AuthResponse?> RefreshAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return null;

        var oldTokenHash = _tokenService.HashToken(refreshToken);

        var newRefreshToken = _tokenService.GenerateRefreshToken();

        var newTokenHash = _tokenService.HashToken(
            newRefreshToken.Token);

        var userId = await _refreshTokenRepository.RotateAsync(
            oldTokenHash,
            newTokenHash,
            newRefreshToken.ExpiresAt);

        if (userId is null)
            return null;

        var user = await _userRepository.GetByIdAsync(userId.Value);

        if (user is null || !user.IsActive)
        {
            await _refreshTokenRepository.RevokeAsync(newTokenHash);
            return null;
        }

        var accessToken = _tokenService.GenerateAccessToken(
            user.UserId,
            user.FullName,
            user.Email,
            user.Role);

        return new AuthResponse
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            AccessToken = accessToken.Token,
            AccessTokenExpiresAt = accessToken.ExpiresAt,
            RefreshToken = newRefreshToken.Token,
            RefreshTokenExpiresAt = newRefreshToken.ExpiresAt
        };
    }

    public async Task LogoutAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return;

        var tokenHash = _tokenService.HashToken(refreshToken);

        await _refreshTokenRepository.RevokeAsync(tokenHash);
    }

    private async Task SendVerificationAsync(User user)
    {
        var token = Convert.ToHexString(
            RandomNumberGenerator.GetBytes(32));

        var tokenHash = _tokenService.HashToken(token);

        var expiresAt = DateTime.UtcNow.AddHours(24);

        await _emailVerificationRepository.SaveAsync(
            user.UserId,
            tokenHash,
            expiresAt);

        var backendUrl = _configuration["Email:BackendBaseUrl"]
            ?? throw new InvalidOperationException(
                "Backend base URL is missing.");

        var verificationUrl =
            $"{backendUrl.TrimEnd('/')}/api/auth/verify-email?token=" +
            Uri.EscapeDataString(token);

        await _emailSender.SendVerificationEmailAsync(
            user.Email,
            verificationUrl);
    }

    public async Task<bool> VerifyEmailAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        var tokenHash = _tokenService.HashToken(token);

        Console.WriteLine($"Received token length: {token.Length}");
        Console.WriteLine($"Received token hash length: {tokenHash.Length}");

        var userId = await _emailVerificationRepository.VerifyAsync(tokenHash);

        Console.WriteLine($"Matched UserId: {userId}");

        return userId.HasValue;
    }

}
