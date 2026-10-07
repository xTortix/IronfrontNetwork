using Ironfront.Matchmaking.Domain.Enums;
using System.Text.Json;


namespace Ironfront.Matchmaking.Domain.Entities;

public sealed class BattleMatch
{
    public const int MaxBattleServerInstanceIdLength = 128;
    public const int MaxModeIdLength = 64;
    public const int MaxMapIdLength = 128;
    public const int MaxFailureReasonLength = 1024;
    public const int MaxRulesSnapshotJsonLength = 64 * 1024;
    
    
    
    private BattleMatch()
    {
    }

    public Guid MatchId { get; private set; }

    public string BattleServerInstanceId { get; private set; } =
        string.Empty;

    public string ModeId { get; private set; } =
        string.Empty;

    public int ModeRevision { get; private set; }

    public string RulesSnapshotJson { get; private set; } =
        string.Empty;
    
    public string MapId { get; private set; } =
        string.Empty;

    public int ExpectedPlayerCount { get; private set; }

    public BattleMatchStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public DateTime? WaitingForPlayersAtUtc { get; private set; }

    public DateTime? StartedAtUtc { get; private set; }

    public DateTime? FinishedAtUtc { get; private set; }

    public string? FailureReason { get; private set; }

    public static BattleMatch Create(
        string battleServerInstanceId,
        string modeId,
        int modeRevision,
        string rulesSnapshotJson,
        string mapId,
        int expectedPlayerCount,
        DateTime utcNow)
    {
        if (expectedPlayerCount is < 1 or > 128)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expectedPlayerCount),
                "Expected player count must be between 1 and 128.");
        }
        
        if (modeRevision < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(modeRevision),
                "Mode revision must be at least 1.");
        }

        DateTime normalizedUtcNow = NormalizeUtc(utcNow);

        return new BattleMatch
        {
            MatchId = Guid.NewGuid(),

            BattleServerInstanceId = NormalizeValue(
                battleServerInstanceId,
                nameof(battleServerInstanceId),
                MaxBattleServerInstanceIdLength),

            ModeId = NormalizeValue(
                modeId,
                nameof(modeId),
                MaxModeIdLength),

            ModeRevision = modeRevision,

            RulesSnapshotJson = NormalizeRulesSnapshotJson(
                rulesSnapshotJson),
            
            MapId = NormalizeValue(
                mapId,
                nameof(mapId),
                MaxMapIdLength),

            ExpectedPlayerCount = expectedPlayerCount,

            Status = BattleMatchStatus.Provisioning,

            CreatedAtUtc = normalizedUtcNow,
            UpdatedAtUtc = normalizedUtcNow
        };
    }

    public void MarkWaitingForPlayers(
        DateTime utcNow)
    {
        if (Status == BattleMatchStatus.WaitingForPlayers)
            return;

        if (Status != BattleMatchStatus.Provisioning)
        {
            throw new InvalidOperationException(
                $"Match '{MatchId}' cannot enter WaitingForPlayers from '{Status}'.");
        }

        DateTime normalizedUtcNow = NormalizeUtc(utcNow);

        Status = BattleMatchStatus.WaitingForPlayers;
        WaitingForPlayersAtUtc = normalizedUtcNow;
        UpdatedAtUtc = normalizedUtcNow;
    }

    public void MarkRunning(
        DateTime utcNow)
    {
        if (Status == BattleMatchStatus.Running)
            return;

        if (Status != BattleMatchStatus.WaitingForPlayers)
        {
            throw new InvalidOperationException(
                $"Match '{MatchId}' cannot enter Running from '{Status}'.");
        }

        DateTime normalizedUtcNow = NormalizeUtc(utcNow);

        Status = BattleMatchStatus.Running;
        StartedAtUtc = normalizedUtcNow;
        UpdatedAtUtc = normalizedUtcNow;
    }

    public void MarkFailed(
        string failureReason,
        DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            failureReason);

        if (Status is BattleMatchStatus.Completed or
            BattleMatchStatus.Cancelled)
        {
            throw new InvalidOperationException(
                $"Match '{MatchId}' cannot fail from '{Status}'.");
        }

        string normalizedFailureReason =
            failureReason.Trim();

        if (normalizedFailureReason.Length >
            MaxFailureReasonLength)
        {
            normalizedFailureReason =
                normalizedFailureReason[..MaxFailureReasonLength];
        }

        DateTime normalizedUtcNow = NormalizeUtc(utcNow);

        Status = BattleMatchStatus.Failed;
        FailureReason = normalizedFailureReason;
        FinishedAtUtc = normalizedUtcNow;
        UpdatedAtUtc = normalizedUtcNow;
    }

    public void MarkCancelled(
        string? reason,
        DateTime utcNow)
    {
        if (Status == BattleMatchStatus.Cancelled)
            return;

        if (Status is BattleMatchStatus.Completed or
            BattleMatchStatus.Failed)
        {
            throw new InvalidOperationException(
                $"Match '{MatchId}' cannot be cancelled from '{Status}'.");
        }

        if (Status is not BattleMatchStatus.Provisioning and
            not BattleMatchStatus.WaitingForPlayers)
        {
            throw new InvalidOperationException(
                $"Match '{MatchId}' cannot be cancelled from '{Status}'.");
        }

        DateTime normalizedUtcNow = NormalizeUtc(utcNow);

        Status = BattleMatchStatus.Cancelled;

        FailureReason = NormalizeOptionalReason(reason);

        FinishedAtUtc = normalizedUtcNow;
        UpdatedAtUtc = normalizedUtcNow;
    }
    
    private static string? NormalizeOptionalReason(
        string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return null;

        string normalizedReason = reason.Trim();

        if (normalizedReason.Length >
            MaxFailureReasonLength)
        {
            normalizedReason =
                normalizedReason[..MaxFailureReasonLength];
        }

        return normalizedReason;
    }
    
    private static string NormalizeValue(
        string value,
        string parameterName,
        int maxLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            value,
            parameterName);

        string normalizedValue = value.Trim();

        if (normalizedValue.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                $"Value must not exceed {maxLength} characters.");
        }

        return normalizedValue;
    }

    private static DateTime NormalizeUtc(
        DateTime value)
    {
        return value.Kind == DateTimeKind.Utc
            ? value
            : value.ToUniversalTime();
    }
    
    private static string NormalizeRulesSnapshotJson(
        string rulesSnapshotJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            rulesSnapshotJson);

        string normalizedJson =
            rulesSnapshotJson.Trim();

        if (normalizedJson.Length >
            MaxRulesSnapshotJsonLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rulesSnapshotJson),
                $"Rules snapshot must not exceed " +
                $"{MaxRulesSnapshotJsonLength} characters.");
        }

        try
        {
            using JsonDocument document =
                JsonDocument.Parse(normalizedJson);

            if (document.RootElement.ValueKind !=
                JsonValueKind.Object)
            {
                throw new ArgumentException(
                    "Rules snapshot must contain a JSON object.",
                    nameof(rulesSnapshotJson));
            }

            return document.RootElement.GetRawText();
        }
        catch (JsonException exception)
        {
            throw new ArgumentException(
                "Rules snapshot must contain valid JSON.",
                nameof(rulesSnapshotJson),
                exception);
        }
    }
}