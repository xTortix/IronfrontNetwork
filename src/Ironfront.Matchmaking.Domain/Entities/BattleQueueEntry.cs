using Ironfront.Matchmaking.Domain.Enums;

namespace Ironfront.Matchmaking.Domain.Entities;

public sealed class BattleQueueEntry
{
    public const int MaxModeIdLength = 64;
    public const int MaxInitialVehicleIdLength = 128;
    public const int MaxVehicleClassLength = 64;
    public const int MaxFailureReasonLength = 1024;

    private BattleQueueEntry()
    {
    }

    public Guid QueueEntryId { get; private set; }

    /*
     * UserService bleibt Eigentümer der User-Daten.
     * Matchmaking speichert nur die externe UserId.
     */
    public Guid UserId { get; private set; }

    /*
     * Die DeckId wird später bei JoinQueue im Gateway gegen UserService
     * validiert. Matchmaking speichert sie als Snapshot-Referenz,
     * ohne auf UserService-Tabellen zuzugreifen.
     */
    public Guid DeckId { get; private set; }

    public string ModeId { get; private set; } =
        string.Empty;

    /*
     * Der Spieler queued immer gegen eine konkrete Modus-Revision.
     * Ändert Warlabs später Regeln, bleibt die bestehende Queue
     * reproduzierbar auf ihrer ursprünglichen Revision.
     */
    public int ModeRevision { get; private set; }

    public string InitialVehicleId { get; private set; } =
        string.Empty;

    /*
     * BR-Snapshot zum Zeitpunkt des Queue-Beitritts.
     *
     * 3.7 BR = 37
     * 10.3 BR = 103
     */
    public byte VehicleBattleRatingTenths { get; private set; }

    /*
     * Beispiel:
     * LightTank, MediumTank, HeavyTank
     *
     * Das ist absichtlich ein Snapshot. Der Matchmaker muss nicht
     * bei jeder Kandidatensuche erneut externe GameData abfragen.
     */
    public string VehicleClass { get; private set; } =
        string.Empty;

    public BattleQueueEntryStatus Status { get; private set; }
    
    /*
     * Wird gesetzt, sobald der Matchmaker diesen Queue-Eintrag
     * atomar einem echten BattleMatch zugeordnet hat.
     */
    public Guid? MatchId { get; private set; }

    public DateTime QueuedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public DateTime? MatchedAtUtc { get; private set; }

    public DateTime? CancelledAtUtc { get; private set; }

    public DateTime? FailedAtUtc { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    public string? FailureReason { get; private set; }

    public bool IsActive =>
        Status is BattleQueueEntryStatus.Queued or
        BattleQueueEntryStatus.Matched;

    public static BattleQueueEntry Create(
        Guid userId,
        Guid deckId,
        string modeId,
        int modeRevision,
        string initialVehicleId,
        byte vehicleBattleRatingTenths,
        string vehicleClass,
        DateTime utcNow)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User id must not be empty.",
                nameof(userId));
        }

        if (deckId == Guid.Empty)
        {
            throw new ArgumentException(
                "Deck id must not be empty.",
                nameof(deckId));
        }

        if (modeRevision < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(modeRevision),
                "Mode revision must be at least 1.");
        }

        if (vehicleBattleRatingTenths == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(vehicleBattleRatingTenths),
                "Vehicle battle rating must be greater than zero.");
        }

        DateTime normalizedUtcNow =
            NormalizeUtc(utcNow);

        return new BattleQueueEntry
        {
            QueueEntryId = Guid.NewGuid(),

            UserId = userId,
            DeckId = deckId,

            ModeId = NormalizeRequiredValue(
                modeId,
                nameof(modeId),
                MaxModeIdLength),

            ModeRevision = modeRevision,

            InitialVehicleId = NormalizeRequiredValue(
                initialVehicleId,
                nameof(initialVehicleId),
                MaxInitialVehicleIdLength),

            VehicleBattleRatingTenths =
                vehicleBattleRatingTenths,

            VehicleClass = NormalizeRequiredValue(
                vehicleClass,
                nameof(vehicleClass),
                MaxVehicleClassLength),

            Status = BattleQueueEntryStatus.Queued,

            QueuedAtUtc = normalizedUtcNow,
            UpdatedAtUtc = normalizedUtcNow
        };
    }

    public void MarkMatched(
        Guid matchId,
        DateTime utcNow)
    {
        if (matchId == Guid.Empty)
        {
            throw new ArgumentException(
                "Match id must not be empty.",
                nameof(matchId));
        }

        if (Status == BattleQueueEntryStatus.Matched &&
            MatchId == matchId)
        {
            return;
        }

        if (Status != BattleQueueEntryStatus.Queued)
        {
            throw new InvalidOperationException(
                $"Queue entry '{QueueEntryId:D}' cannot be matched from status '{Status}'.");
        }

        DateTime normalizedUtcNow =
            NormalizeUtc(utcNow);

        Status = BattleQueueEntryStatus.Matched;
        MatchId = matchId;

        MatchedAtUtc = normalizedUtcNow;
        UpdatedAtUtc = normalizedUtcNow;
    }

    public void Cancel(
        DateTime utcNow)
    {
        if (Status == BattleQueueEntryStatus.Cancelled)
        {
            return;
        }

        if (Status != BattleQueueEntryStatus.Queued)
        {
            throw new InvalidOperationException(
                $"Queue entry '{QueueEntryId:D}' cannot be cancelled from status '{Status}'.");
        }

        DateTime normalizedUtcNow =
            NormalizeUtc(utcNow);

        Status = BattleQueueEntryStatus.Cancelled;

        CancelledAtUtc = normalizedUtcNow;
        UpdatedAtUtc = normalizedUtcNow;
    }

    public void MarkFailed(
        string failureReason,
        DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            failureReason);

        if (Status == BattleQueueEntryStatus.Failed)
        {
            return;
        }

        if (Status != BattleQueueEntryStatus.Queued)
        {
            throw new InvalidOperationException(
                $"Queue entry '{QueueEntryId:D}' cannot fail from status '{Status}'.");
        }

        DateTime normalizedUtcNow =
            NormalizeUtc(utcNow);

        Status = BattleQueueEntryStatus.Failed;

        FailureReason = NormalizeFailureReason(
            failureReason);

        FailedAtUtc = normalizedUtcNow;
        UpdatedAtUtc = normalizedUtcNow;
    }

    public void MarkCompleted(
        DateTime utcNow)
    {
        if (Status == BattleQueueEntryStatus.Completed)
        {
            return;
        }

        if (Status != BattleQueueEntryStatus.Matched)
        {
            throw new InvalidOperationException(
                $"Queue entry '{QueueEntryId:D}' cannot complete from status '{Status}'.");
        }

        DateTime normalizedUtcNow =
            NormalizeUtc(utcNow);

        Status = BattleQueueEntryStatus.Completed;

        CompletedAtUtc = normalizedUtcNow;
        UpdatedAtUtc = normalizedUtcNow;
    }

    private static string NormalizeRequiredValue(
        string value,
        string parameterName,
        int maximumLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            value,
            parameterName);

        string normalizedValue =
            value.Trim();

        if (normalizedValue.Length > maximumLength)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                $"Value must not exceed {maximumLength} characters.");
        }

        return normalizedValue;
    }

    private static string NormalizeFailureReason(
        string failureReason)
    {
        string normalizedFailureReason =
            failureReason.Trim();

        if (normalizedFailureReason.Length >
            MaxFailureReasonLength)
        {
            normalizedFailureReason =
                normalizedFailureReason[
                    ..MaxFailureReasonLength];
        }

        return normalizedFailureReason;
    }

    private static DateTime NormalizeUtc(
        DateTime value)
    {
        return value.Kind == DateTimeKind.Utc
            ? value
            : value.ToUniversalTime();
    }
}