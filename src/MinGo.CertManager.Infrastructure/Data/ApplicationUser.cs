using Microsoft.AspNetCore.Identity;
using MinGo.CertManager.Core.Entities;

namespace MinGo.CertManager.Infrastructure.Data;

public class ApplicationUser : IdentityUser
{
    public UserStatus Status { get; set; } = UserStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
