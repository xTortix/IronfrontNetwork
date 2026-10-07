using System.Text.Json;
using Ironfront.Matchmaking.Domain.Enums;

namespace Ironfront.Matchmaking.Domain.Entities;

public sealed class BattleServerCommand
{
    public const int MaxBattleServerInstanceIdLength = 128;
    public const int MaxCommandTypeLength = 64;
    public const int MaxFailureReasonLength = 1024;

    private BattleServerCommand()
    {
    }

    public Guid CommandId { get; private set; }

    public string BattleServerInstanceId { get; private set; } =
        string.Empty;
    
    public Guid? MatchId { get; private set; }
    
    public string CommandType { get; private set; } =
        string.Empty;

    public string PayloadJson { get; private set; } =
        string.Empty;

    public BattleServerCommandState State { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? DeliveredAtUtc { get; private set; }

    public DateTime? AcknowledgedAtUtc { get; private set; }

    public DateTime? FailedAtUtc { get; private set; }

    public string? FailureReason { get; private set; }

    public static BattleServerCommand Create(
        string battleServerInstanceId,
        string commandType,
        string payloadJson,
        DateTime utcNow,
        Guid? matchId = null)
    {
        return new BattleServerCommand
        {
            CommandId = Guid.NewGuid(),

            BattleServerInstanceId = NormalizeValue(
                battleServerInstanceId,
                nameof(battleServerInstanceId),
                MaxBattleServerInstanceIdLength),

            CommandType = NormalizeValue(
                commandType,
                nameof(commandType),
                MaxCommandTypeLength),

            PayloadJson = NormalizePayloadJson(
                payloadJson),
            
            MatchId = matchId,
            
            State = BattleServerCommandState.Pending,
            
            CreatedAtUtc = NormalizeUtc(utcNow)
        };
    }

    public void MarkDelivered(DateTime utcNow)
    {
        if (State is BattleServerCommandState.Acknowledged or
            BattleServerCommandState.Failed or
            BattleServerCommandState.Cancelled)
        {
            return;
        }

        State = BattleServerCommandState.Delivered;

        /*
         * This remains the timestamp of the first delivery.
         * A Delivered command is intentionally still pollable:
         * if an acknowledgement is lost, the BattleServer receives
         * the same CommandId again and can handle it idempotently.
         */
        DeliveredAtUtc ??= NormalizeUtc(utcNow);
    }

    public void Acknowledge(DateTime utcNow)
    {
        if (State == BattleServerCommandState.Acknowledged)
            return;

        if (State is BattleServerCommandState.Failed or
            BattleServerCommandState.Cancelled)
        {
            throw new InvalidOperationException(
                $"Command '{CommandId}' cannot be acknowledged from state '{State}'.");
        }

        State = BattleServerCommandState.Acknowledged;

        AcknowledgedAtUtc = NormalizeUtc(utcNow);

        FailureReason = null;
        FailedAtUtc = null;
    }

    public void Fail(
        string failureReason,
        DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            failureReason);

        if (State == BattleServerCommandState.Acknowledged)
        {
            throw new InvalidOperationException(
                $"Command '{CommandId}' was already acknowledged.");
        }

        string normalizedReason = failureReason.Trim();

        if (normalizedReason.Length >
            MaxFailureReasonLength)
        {
            normalizedReason = normalizedReason[
                ..MaxFailureReasonLength];
        }

        State = BattleServerCommandState.Failed;

        FailureReason = normalizedReason;

        FailedAtUtc = NormalizeUtc(utcNow);
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

    private static string NormalizePayloadJson(
        string payloadJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            payloadJson,
            nameof(payloadJson));

        try
        {
            using JsonDocument document =
                JsonDocument.Parse(payloadJson);

            if (document.RootElement.ValueKind !=
                JsonValueKind.Object)
            {
                throw new ArgumentException(
                    "Command payload JSON must contain an object.",
                    nameof(payloadJson));
            }

            return payloadJson;
        }
        catch (JsonException exception)
        {
            throw new ArgumentException(
                "Command payload must be valid JSON.",
                nameof(payloadJson),
                exception);
        }
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc
            ? value
            : value.ToUniversalTime();
    }
}