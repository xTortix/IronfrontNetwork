using Ironfront.Matchmaking.Domain.Enums;

namespace Ironfront.Matchmaking.Domain.Entities;

public sealed class BattleServerSlot
{
    public const int MaxServerInstanceIdLength = 128;
    public const int MaxHostIdLength = 128;
    public const int MaxRegionLength = 64;
    public const int MaxPublicHostLength = 255;
    public const int MaxBuildVersionLength = 64;
    public const int MaxCatalogVersionLength = 64;

    private BattleServerSlot()
    {
    }

    public string ServerInstanceId { get; private set; } = string.Empty;

    public string HostId { get; private set; } = string.Empty;

    public string Region { get; private set; } = string.Empty;

    public string PublicHost { get; private set; } = string.Empty;

    public int PublicPort { get; private set; }

    public string BuildVersion { get; private set; } = string.Empty;

    public string CatalogVersion { get; private set; } = string.Empty;

    public BattleServerSlotStatus Status { get; private set; }

    public int PlayerCount { get; private set; }

    public int MaxPlayers { get; private set; }

    public Guid? ActiveMatchId { get; private set; }

    public DateTime RegisteredAtUtc { get; private set; }

    public DateTime LastHeartbeatUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static BattleServerSlot Create(
        string serverInstanceId,
        string hostId,
        string region,
        string publicHost,
        int publicPort,
        string buildVersion,
        string catalogVersion,
        int maxPlayers,
        DateTime utcNow)
    {
        DateTime normalizedUtcNow = NormalizeUtc(utcNow);

        return new BattleServerSlot
        {
            ServerInstanceId = NormalizeValue(
                serverInstanceId,
                nameof(serverInstanceId),
                MaxServerInstanceIdLength),

            HostId = NormalizeValue(
                hostId,
                nameof(hostId),
                MaxHostIdLength),

            Region = NormalizeValue(
                region,
                nameof(region),
                MaxRegionLength),

            PublicHost = NormalizeValue(
                publicHost,
                nameof(publicHost),
                MaxPublicHostLength),

            PublicPort = ValidatePort(publicPort),

            BuildVersion = NormalizeValue(
                buildVersion,
                nameof(buildVersion),
                MaxBuildVersionLength),

            CatalogVersion = NormalizeValue(
                catalogVersion,
                nameof(catalogVersion),
                MaxCatalogVersionLength),

            MaxPlayers = ValidateMaxPlayers(maxPlayers),

            PlayerCount = 0,
            Status = BattleServerSlotStatus.Ready,

            RegisteredAtUtc = normalizedUtcNow,
            LastHeartbeatUtc = normalizedUtcNow,
            UpdatedAtUtc = normalizedUtcNow
        };
    }

    public void RefreshRegistration(
        string hostId,
        string region,
        string publicHost,
        int publicPort,
        string buildVersion,
        string catalogVersion,
        int maxPlayers,
        DateTime utcNow)
    {
        DateTime normalizedUtcNow = NormalizeUtc(utcNow);

        HostId = NormalizeValue(
            hostId,
            nameof(hostId),
            MaxHostIdLength);

        Region = NormalizeValue(
            region,
            nameof(region),
            MaxRegionLength);

        PublicHost = NormalizeValue(
            publicHost,
            nameof(publicHost),
            MaxPublicHostLength);

        PublicPort = ValidatePort(publicPort);

        BuildVersion = NormalizeValue(
            buildVersion,
            nameof(buildVersion),
            MaxBuildVersionLength);

        CatalogVersion = NormalizeValue(
            catalogVersion,
            nameof(catalogVersion),
            MaxCatalogVersionLength);

        MaxPlayers = ValidateMaxPlayers(maxPlayers);

        /*
         * A slot that had previously gone offline may become
         * available again after a clean process restart.
         *
         * We deliberately do not overwrite Provisioning, Running
         * or any other active lifecycle state here. Later the
         * Match Coordinator will own those transitions.
         */
        if (ActiveMatchId is null &&
            Status is BattleServerSlotStatus.Offline or
                BattleServerSlotStatus.Unhealthy)
        {
            Status = BattleServerSlotStatus.Ready;
        }

        if (ActiveMatchId is null)
        {
            PlayerCount = 0;
        }

        LastHeartbeatUtc = normalizedUtcNow;
        UpdatedAtUtc = normalizedUtcNow;
    }

    public void RecordHeartbeat(
        int playerCount,
        DateTime utcNow)
    {
        if (playerCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(playerCount),
                "Player count must not be negative.");
        }

        if (playerCount > MaxPlayers)
        {
            throw new ArgumentOutOfRangeException(
                nameof(playerCount),
                $"Player count cannot exceed the slot maximum of {MaxPlayers}.");
        }

        DateTime normalizedUtcNow = NormalizeUtc(utcNow);

        PlayerCount = playerCount;
        LastHeartbeatUtc = normalizedUtcNow;
        UpdatedAtUtc = normalizedUtcNow;
    }

    private static string NormalizeValue(
        string value,
        string parameterName,
        int maxLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            value,
            parameterName);

        string normalized = value.Trim();

        if (normalized.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                $"Value must not exceed {maxLength} characters.");
        }

        return normalized;
    }

    private static int ValidatePort(int port)
    {
        if (port is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(
                nameof(port),
                "Public port must be between 1 and 65535.");
        }

        return port;
    }

    private static int ValidateMaxPlayers(int maxPlayers)
    {
        if (maxPlayers is < 1 or > 128)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxPlayers),
                "Maximum players must be between 1 and 128.");
        }

        return maxPlayers;
    }

    public void ReleaseReservedMatch(
        Guid matchId,
        DateTime utcNow)
    {
        if (matchId == Guid.Empty)
        {
            throw new ArgumentException(
                "Match id must not be empty.",
                nameof(matchId));
        }

        if (ActiveMatchId != matchId)
        {
            throw new InvalidOperationException(
                $"Battle server slot '{ServerInstanceId}' is not assigned " +
                $"to match '{matchId}'.");
        }

        if (Status is not BattleServerSlotStatus.Provisioning and
            not BattleServerSlotStatus.WaitingForPlayers)
        {
            throw new InvalidOperationException(
                $"Battle server slot '{ServerInstanceId}' cannot release " +
                $"a match from state '{Status}'.");
        }

        ActiveMatchId = null;
        PlayerCount = 0;
        Status = BattleServerSlotStatus.Ready;
        UpdatedAtUtc = NormalizeUtc(utcNow);
    }
    
    public void MarkOffline(
        DateTime utcNow)
    {
        if (ActiveMatchId.HasValue)
        {
            throw new InvalidOperationException(
                $"Battle server slot '{ServerInstanceId}' cannot be marked " +
                "offline while it still owns an active match.");
        }

        if (Status == BattleServerSlotStatus.Offline)
            return;

        Status = BattleServerSlotStatus.Offline;
        PlayerCount = 0;
        UpdatedAtUtc = NormalizeUtc(utcNow);
    }

    public void MarkOfflineAndReleaseMatch(
        Guid matchId,
        DateTime utcNow)
    {
        if (matchId == Guid.Empty)
        {
            throw new ArgumentException(
                "Match id must not be empty.",
                nameof(matchId));
        }

        if (Status == BattleServerSlotStatus.Offline &&
            ActiveMatchId is null)
        {
            return;
        }

        if (ActiveMatchId != matchId)
        {
            throw new InvalidOperationException(
                $"Battle server slot '{ServerInstanceId}' is not assigned " +
                $"to match '{matchId}'.");
        }

        ActiveMatchId = null;
        PlayerCount = 0;
        Status = BattleServerSlotStatus.Offline;
        UpdatedAtUtc = NormalizeUtc(utcNow);
    }
    
    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc
            ? value
            : value.ToUniversalTime();
    }
    
    public void ReserveForMatch(
        Guid matchId,
        int expectedPlayerCount,
        DateTime utcNow)
    {
        if (matchId == Guid.Empty)
        {
            throw new ArgumentException(
                "Match id must not be empty.",
                nameof(matchId));
        }

        if (expectedPlayerCount is < 1 or > 128)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expectedPlayerCount),
                "Expected player count must be between 1 and 128.");
        }

        if (expectedPlayerCount > MaxPlayers)
        {
            throw new InvalidOperationException(
                $"Battle server slot '{ServerInstanceId}' supports at most " +
                $"{MaxPlayers} players, but the match requires " +
                $"{expectedPlayerCount} players.");
        }

        if (ActiveMatchId == matchId &&
            Status is BattleServerSlotStatus.Provisioning or
                BattleServerSlotStatus.WaitingForPlayers)
        {
            return;
        }

        if (ActiveMatchId.HasValue)
        {
            throw new InvalidOperationException(
                $"Battle server slot '{ServerInstanceId}' is already assigned " +
                $"to match '{ActiveMatchId}'.");
        }

        if (Status != BattleServerSlotStatus.Ready)
        {
            throw new InvalidOperationException(
                $"Battle server slot '{ServerInstanceId}' cannot be reserved " +
                $"from state '{Status}'.");
        }

        ActiveMatchId = matchId;
        Status = BattleServerSlotStatus.Provisioning;
        UpdatedAtUtc = NormalizeUtc(utcNow);
    }
    
    public void MarkMatchWaitingForPlayers(
        Guid matchId,
        DateTime utcNow)
    {
        if (matchId == Guid.Empty)
        {
            throw new ArgumentException(
                "Match id must not be empty.",
                nameof(matchId));
        }

        if (ActiveMatchId != matchId)
        {
            throw new InvalidOperationException(
                $"Battle server slot '{ServerInstanceId}' is not assigned to match '{matchId}'.");
        }

        if (Status == BattleServerSlotStatus.WaitingForPlayers)
            return;

        if (Status != BattleServerSlotStatus.Provisioning)
        {
            throw new InvalidOperationException(
                $"Battle server slot '{ServerInstanceId}' cannot enter WaitingForPlayers from state '{Status}'.");
        }

        Status = BattleServerSlotStatus.WaitingForPlayers;
        UpdatedAtUtc = NormalizeUtc(utcNow);
    }
}