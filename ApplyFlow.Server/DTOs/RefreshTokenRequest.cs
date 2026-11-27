
using System.ComponentModel.DataAnnotations;

namespace ApplyFlow.Server.DTOs;

public sealed class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}