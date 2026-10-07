using Ironfront.Matchmaking.Domain.Enums;

namespace Ironfront.Matchmaking.Domain.Entities;

public sealed class BattleMatchPlayer
{
    public const int MaxInitialVehicleIdLength = 128;
    public const byte MaxTeamId = 127;

    private BattleMatchPlayer()
    {
    }

    public Guid MatchPlayerId { get; private set; }

    public Guid MatchId { get; private set; }

    public Guid UserId { get; private set; }

    /*
     * Technische Team-ID innerhalb dieses Matches.
     *
     * Die IDs kommen aus dem versionierten Mode-Rule-Snapshot und
     * sind zero-based durchnummeriert: 0, 1, 2 ...
     * Anzeigenamen wie Alpha/Bravo bleiben reine Display-Daten.
     */
    public byte TeamId { get; private set; } = 0;

    /*
     * Platz innerhalb des jeweiligen Teams.
     *
     * Bei einem 1v1-Modus ist dieser Wert für beide Spieler 0.
     * Bei 5v5 besitzt jedes Team die Slots 0 bis 4.
     */
    public int TeamSlotIndex { get; private set; }

    /*
     * Für den ersten Vertical Slice speichern wir das Fahrzeug,
     * das der BattleServer beim späteren Join serverseitig spawnen soll.
     *
     * Ein vollständiger Deck-/Lineup-Snapshot kommt später dazu,
     * bevor mehrere Respawns und Reservefahrzeuge umgesetzt werden.
     */
    public string InitialVehicleId { get; private set; } =
        string.Empty;

    /*
     * Eindeutiger globaler Spielerplatz innerhalb dieses Matches.
     *
     * Der BattleServer kann diesen später für deterministische
     * Runtime-Actor- und Spawn-Zuordnungen verwenden.
     */
    public int PlayerSlotIndex { get; private set; }

    public BattleMatchPlayerStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public DateTime? ConnectedAtUtc { get; private set; }

    public DateTime? DisconnectedAtUtc { get; private set; }

    public DateTime? LeftAtUtc { get; private set; }

    public static BattleMatchPlayer Create(
        Guid matchId,
        Guid userId,
        string initialVehicleId,
        byte teamId,
        int teamSlotIndex,
        int playerSlotIndex,
        DateTime utcNow)
    {
        if (matchId == Guid.Empty)
        {
            throw new ArgumentException(
                "Match id must not be empty.",
                nameof(matchId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User id must not be empty.",
                nameof(userId));
        }

        if (teamSlotIndex is < 0 or > 63)
        {
            throw new ArgumentOutOfRangeException(
                nameof(teamSlotIndex),
                "Team slot index must be between 0 and 63.");
        }

        if (playerSlotIndex is < 0 or > 127)
        {
            throw new ArgumentOutOfRangeException(
                nameof(playerSlotIndex),
                "Player slot index must be between 0 and 127.");
        }

        DateTime normalizedUtcNow =
            NormalizeUtc(utcNow);

        return new BattleMatchPlayer
        {
            MatchPlayerId = Guid.NewGuid(),

            MatchId = matchId,

            UserId = userId,

            TeamId = teamId,

            TeamSlotIndex = teamSlotIndex,

            InitialVehicleId = NormalizeVehicleId(
                initialVehicleId),

            PlayerSlotIndex = playerSlotIndex,

            Status = BattleMatchPlayerStatus.Reserved,

            CreatedAtUtc = normalizedUtcNow,
            UpdatedAtUtc = normalizedUtcNow
        };
    }

    public bool CanReceiveJoinTicket =>
        Status is BattleMatchPlayerStatus.Reserved or
        BattleMatchPlayerStatus.Disconnected;

    public void MarkConnected(
        DateTime utcNow)
    {
        if (Status == BattleMatchPlayerStatus.Connected)
        {
            return;
        }

        if (Status is not BattleMatchPlayerStatus.Reserved and
            not BattleMatchPlayerStatus.Disconnected)
        {
            throw new InvalidOperationException(
                $"Match player '{MatchPlayerId}' cannot connect from '{Status}'.");
        }

        DateTime normalizedUtcNow =
            NormalizeUtc(utcNow);

        Status = BattleMatchPlayerStatus.Connected;

        ConnectedAtUtc = normalizedUtcNow;
        DisconnectedAtUtc = null;

        UpdatedAtUtc = normalizedUtcNow;
    }

    public void MarkDisconnected(
        DateTime utcNow)
    {
        if (Status == BattleMatchPlayerStatus.Disconnected)
        {
            return;
        }

        if (Status != BattleMatchPlayerStatus.Connected)
        {
            throw new InvalidOperationException(
                $"Match player '{MatchPlayerId}' cannot disconnect from '{Status}'.");
        }

        DateTime normalizedUtcNow =
            NormalizeUtc(utcNow);

        Status = BattleMatchPlayerStatus.Disconnected;

        DisconnectedAtUtc = normalizedUtcNow;
        UpdatedAtUtc = normalizedUtcNow;
    }

    public void MarkLeft(
        DateTime utcNow)
    {
        if (Status == BattleMatchPlayerStatus.Left)
        {
            return;
        }

        DateTime normalizedUtcNow =
            NormalizeUtc(utcNow);

        Status = BattleMatchPlayerStatus.Left;

        LeftAtUtc = normalizedUtcNow;
        UpdatedAtUtc = normalizedUtcNow;
    }
    

    private static string NormalizeVehicleId(
        string vehicleId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            vehicleId);

        string normalizedVehicleId =
            vehicleId.Trim();

        if (normalizedVehicleId.Length >
            MaxInitialVehicleIdLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(vehicleId),
                $"Initial vehicle id must not exceed " +
                $"{MaxInitialVehicleIdLength} characters.");
        }

        return normalizedVehicleId;
    }

    private static DateTime NormalizeUtc(
        DateTime value)
    {
        return value.Kind == DateTimeKind.Utc
            ? value
            : value.ToUniversalTime();
    }
}