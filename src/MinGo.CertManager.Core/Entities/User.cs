namespace MinGo.CertManager.Core.Entities;

public enum UserRole
{
    Admin = 0,
    User = 1
}

public enum UserStatus
{
    Active = 0,
    Disabled = 1
}

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public UserStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
