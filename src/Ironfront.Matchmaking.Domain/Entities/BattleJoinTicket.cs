using Ironfront.Matchmaking.Domain.Enums;

namespace Ironfront.Matchmaking.Domain.Entities;

public sealed class BattleJoinTicket
{
    public const int SecretHashLength = 32;
    public const int MaxBattleServerInstanceIdLength = 128;

    private BattleJoinTicket()
    {
    }

    public Guid TicketId { get; private set; }

    public Guid MatchId { get; private set; }

    public Guid MatchPlayerId { get; private set; }

    /*
     * Das Ticket ist immer an genau einen BattleServer-Slot gebunden.
     * Ein Ticket für Slot A darf niemals auf Slot B akzeptiert werden.
     */
    public string BattleServerInstanceId { get; private set; } =
        string.Empty;

    /*
     * Niemals das rohe Secret persistieren.
     * Hier liegt ausschließlich SHA-256(secret).
     */
    public byte[] SecretHash { get; private set; } =
        Array.Empty<byte>();

    public BattleJoinTicketStatus Status { get; private set; }

    public DateTime IssuedAtUtc { get; private set; }

    public DateTime ExpiresAtUtc { get; private set; }

    public DateTime? ConsumedAtUtc { get; private set; }

    public DateTime? ExpiredAtUtc { get; private set; }

    public DateTime? RevokedAtUtc { get; private set; }

    public static BattleJoinTicket Issue(
        Guid matchId,
        Guid matchPlayerId,
        string battleServerInstanceId,
        byte[] secretHash,
        DateTime issuedAtUtc,
        DateTime expiresAtUtc)
    {
        if (matchId == Guid.Empty)
        {
            throw new ArgumentException(
                "Match id must not be empty.",
                nameof(matchId));
        }

        if (matchPlayerId == Guid.Empty)
        {
            throw new ArgumentException(
                "Match player id must not be empty.",
                nameof(matchPlayerId));
        }

        DateTime normalizedIssuedAtUtc =
            NormalizeUtc(issuedAtUtc);

        DateTime normalizedExpiresAtUtc =
            NormalizeUtc(expiresAtUtc);

        if (normalizedExpiresAtUtc <= normalizedIssuedAtUtc)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expiresAtUtc),
                "Ticket expiry must be later than its issue time.");
        }

        return new BattleJoinTicket
        {
            TicketId = Guid.NewGuid(),

            MatchId = matchId,

            MatchPlayerId = matchPlayerId,

            BattleServerInstanceId =
                NormalizeBattleServerInstanceId(
                    battleServerInstanceId),

            SecretHash = NormalizeSecretHash(
                secretHash),

            Status = BattleJoinTicketStatus.Issued,

            IssuedAtUtc = normalizedIssuedAtUtc,
            ExpiresAtUtc = normalizedExpiresAtUtc
        };
    }

    /*
     * Der Hash-Vergleich findet später im Application-Layer mit
     * CryptographicOperations.FixedTimeEquals statt.
     *
     * Diese Methode übernimmt ausschließlich den fachlichen,
     * einmaligen Zustandswechsel.
     */
    public bool TryConsume(
        DateTime utcNow)
    {
        if (Status != BattleJoinTicketStatus.Issued)
            return false;

        DateTime normalizedUtcNow =
            NormalizeUtc(utcNow);

        if (normalizedUtcNow >= ExpiresAtUtc)
        {
            Status = BattleJoinTicketStatus.Expired;
            ExpiredAtUtc = normalizedUtcNow;

            return false;
        }

        Status = BattleJoinTicketStatus.Consumed;
        ConsumedAtUtc = normalizedUtcNow;

        return true;
    }

    public void Revoke(
        DateTime utcNow)
    {
        if (Status is BattleJoinTicketStatus.Consumed or
            BattleJoinTicketStatus.Revoked)
        {
            return;
        }

        DateTime normalizedUtcNow =
            NormalizeUtc(utcNow);

        Status = BattleJoinTicketStatus.Revoked;
        RevokedAtUtc = normalizedUtcNow;
    }

    private static string NormalizeBattleServerInstanceId(
        string battleServerInstanceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            battleServerInstanceId);

        string normalizedValue =
            battleServerInstanceId.Trim();

        if (normalizedValue.Length >
            MaxBattleServerInstanceIdLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(battleServerInstanceId),
                $"Battle server instance id must not exceed " +
                $"{MaxBattleServerInstanceIdLength} characters.");
        }

        return normalizedValue;
    }

    private static byte[] NormalizeSecretHash(
        byte[] secretHash)
    {
        ArgumentNullException.ThrowIfNull(
            secretHash);

        if (secretHash.Length != SecretHashLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(secretHash),
                $"Secret hash must be exactly " +
                $"{SecretHashLength} bytes.");
        }

        return secretHash.ToArray();
    }

    private static DateTime NormalizeUtc(
        DateTime value)
    {
        return value.Kind == DateTimeKind.Utc
            ? value
            : value.ToUniversalTime();
    }
}