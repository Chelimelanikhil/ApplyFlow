namespace ApplyFlow.Server.Models
{
    public sealed class User
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string NormalizedEmail { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "User";
        public bool IsEmailVerified { get; set; }
        public bool IsActive { get; set; }
    }
}
