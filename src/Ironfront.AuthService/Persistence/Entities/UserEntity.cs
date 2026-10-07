using System.Collections.Generic;

namespace Ironfront.AuthService.Persistence.Entities;

public sealed class UserEntity
{
    public Guid Id { get; set; }

    public string Username { get; set; } = "";
    public string NormalizedUsername { get; set; } = "";

    public string Email { get; set; } = "";
    public string NormalizedEmail { get; set; } = "";

    public string PasswordHash { get; set; } = "";

    public DateTime CreatedAtUtc { get; set; }

    public ICollection<RefreshSessionEntity> RefreshSessions { get; set; }
        = new List<RefreshSessionEntity>();
}