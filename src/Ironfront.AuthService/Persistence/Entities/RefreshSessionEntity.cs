namespace Ironfront.AuthService.Persistence.Entities;

public sealed class RefreshSessionEntity
{
    // ID dieser konkreten Refresh-Token-Generation.
    public Guid Id { get; set; }

    // Der Benutzer, dem diese Session gehört.
    public Guid UserId { get; set; }
    public UserEntity User { get; set; } = null!;

    // SHA-256-Hash des echten Refresh Tokens.
    // Der rohe Token wird niemals in PostgreSQL gespeichert.
    public string TokenHash { get; set; } = "";

    // Stabile ID einer Anmeldung bzw. Geräte-Session.
    // Bleibt bei Token-Rotation erhalten.
    public Guid FamilyId { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }

    // Wird gesetzt, wenn dieser konkrete Token nicht mehr genutzt werden darf.
    public DateTime? RevokedAtUtc { get; set; }

    // Wird gesetzt, wenn dieser Token erfolgreich gegen einen neuen rotiert wurde.
    public DateTime? ReplacedAtUtc { get; set; }
    public Guid? ReplacedByTokenId { get; set; }

    // Zum Beispiel: "rotated", "logout", "refresh_token_reuse", "expired".
    public string? RevocationReason { get; set; }

    // Schützt die Token-Rotation gegen zwei zeitgleiche Refresh-Anfragen.
    public Guid ConcurrencyStamp { get; set; }
}