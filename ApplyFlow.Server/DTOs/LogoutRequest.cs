
using System.ComponentModel.DataAnnotations;

namespace ApplyFlow.Server.DTOs;

public sealed class LogoutRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}