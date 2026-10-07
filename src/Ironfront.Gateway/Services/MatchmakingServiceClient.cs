using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ironfront.Shared.Constants;
using Ironfront.Shared.Contracts.Matchmaking;

namespace Ironfront.Gateway.Services;

public sealed class MatchmakingServiceClient
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly ILogger<MatchmakingServiceClient> logger;

    public MatchmakingServiceClient(
        IHttpClientFactory httpClientFactory,
        ILogger<MatchmakingServiceClient> logger)
    {
        this.httpClientFactory = httpClientFactory
            ?? throw new ArgumentNullException(
                nameof(httpClientFactory));

        this.logger = logger
            ?? throw new ArgumentNullException(
                nameof(logger));
    }

    public Task<
        MatchmakingServiceCallResult<
            InternalBattleQueueStatusResponse>>
        JoinQueueAsync(
            InternalJoinBattleQueueRequest request,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return SendAsync<
            InternalBattleQueueStatusResponse>(
            HttpMethod.Post,
            "/internal/v1/battle-queue/join",
            request,
            cancellationToken);
    }

    public Task<
        MatchmakingServiceCallResult<
            InternalBattleQueueStatusResponse>>
        GetQueueStatusAsync(
            Guid userId,
            CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User id must not be empty.",
                nameof(userId));
        }

        return SendAsync<
            InternalBattleQueueStatusResponse>(
            HttpMethod.Get,
            $"/internal/v1/battle-queue/{userId:D}/status",
            body: null,
            cancellationToken);
    }

    public Task<
        MatchmakingServiceCallResult<
            InternalBattleQueueLeaveResponse>>
        LeaveQueueAsync(
            Guid userId,
            CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User id must not be empty.",
                nameof(userId));
        }

        return SendAsync<
            InternalBattleQueueLeaveResponse>(
            HttpMethod.Post,
            $"/internal/v1/battle-queue/{userId:D}/leave",
            body: null,
            cancellationToken);
    }

    public Task<
        MatchmakingServiceCallResult<
            InternalBattleConnectionTicketResponse>>
        IssueConnectionTicketAsync(
            Guid userId,
            CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User id must not be empty.",
                nameof(userId));
        }

        return SendAsync<
            InternalBattleConnectionTicketResponse>(
            HttpMethod.Post,
            $"/internal/v1/battle-queue/{userId:D}/connection-ticket",
            body: null,
            cancellationToken);
    }

    private async Task<MatchmakingServiceCallResult<T>>
        SendAsync<T>(
            HttpMethod method,
            string relativePath,
            object? body,
            CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            HttpClient client =
                httpClientFactory.CreateClient(
                    ServiceNames.MatchmakingService);

            using var request =
                new HttpRequestMessage(
                    method,
                    relativePath);

            if (body is not null)
            {
                request.Content =
                    JsonContent.Create(body);
            }

            using HttpResponseMessage response =
                await client.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                T? payload =
                    await response.Content.ReadFromJsonAsync<T>(
                        cancellationToken:
                            cancellationToken);

                if (payload is null)
                {
                    logger.LogError(
                        "MatchmakingService returned an empty " +
                        "success response for {Method} {Path}.",
                        method,
                        relativePath);

                    return MatchmakingServiceCallResult<T>
                        .InvalidResponse(
                            "matchmaking_invalid_response",
                            "Matchmaking returned an invalid response.");
                }

                return MatchmakingServiceCallResult<T>
                    .Success(payload);
            }

            (
                string errorCode,
                string errorMessage
            ) = await ReadErrorAsync(
                response,
                cancellationToken);

            return response.StatusCode switch
            {
                HttpStatusCode.BadRequest =>
                    MatchmakingServiceCallResult<T>
                        .BadRequest(
                            errorCode,
                            errorMessage),

                HttpStatusCode.Conflict =>
                    MatchmakingServiceCallResult<T>
                        .Conflict(
                            errorCode,
                            errorMessage),

                HttpStatusCode.Unauthorized or
                HttpStatusCode.Forbidden =>
                    MatchmakingServiceCallResult<T>
                        .Unauthorized(
                            "matchmaking_internal_auth_failed",
                            "Matchmaking rejected the internal service request."),

                _ =>
                    MatchmakingServiceCallResult<T>
                        .Unavailable(
                            "matchmaking_unavailable",
                            "Matchmaking is currently unavailable.")
            };
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException exception)
        {
            logger.LogWarning(
                exception,
                "MatchmakingService timed out for {Method} {Path}.",
                method,
                relativePath);

            return MatchmakingServiceCallResult<T>
                .Unavailable(
                    "matchmaking_timeout",
                    "Matchmaking did not respond in time.");
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(
                exception,
                "MatchmakingService was unreachable for {Method} {Path}.",
                method,
                relativePath);

            return MatchmakingServiceCallResult<T>
                .Unavailable(
                    "matchmaking_unreachable",
                    "Matchmaking is currently unavailable.");
        }
    }

    private static async Task<(string ErrorCode, string ErrorMessage)>
        ReadErrorAsync(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
    {
        try
        {
            await using Stream contentStream =
                await response.Content.ReadAsStreamAsync(
                    cancellationToken);

            using JsonDocument document =
                await JsonDocument.ParseAsync(
                    contentStream,
                    cancellationToken:
                        cancellationToken);

            JsonElement root =
                document.RootElement;

            string errorCode =
                root.TryGetProperty(
                    "error",
                    out JsonElement errorElement)
                ? errorElement.GetString()
                    ?? "matchmaking_request_rejected"
                : "matchmaking_request_rejected";

            string errorMessage =
                root.TryGetProperty(
                    "message",
                    out JsonElement messageElement)
                ? messageElement.GetString()
                    ?? "Matchmaking rejected the request."
                : "Matchmaking rejected the request.";

            return (
                errorCode,
                errorMessage);
        }
        catch
        {
            return (
                "matchmaking_request_rejected",
                "Matchmaking rejected the request.");
        }
    }
}

public enum MatchmakingServiceCallStatus : byte
{
    Success = 0,
    BadRequest = 1,
    Conflict = 2,
    Unauthorized = 3,
    Unavailable = 4,
    InvalidResponse = 5
}

public sealed record MatchmakingServiceCallResult<T>(
    MatchmakingServiceCallStatus Status,
    T? Data,
    string ErrorCode,
    string ErrorMessage)
    where T : class
{
    public static MatchmakingServiceCallResult<T> Success(
        T data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return new(
            MatchmakingServiceCallStatus.Success,
            data,
            string.Empty,
            string.Empty);
    }

    public static MatchmakingServiceCallResult<T> BadRequest(
        string errorCode,
        string errorMessage)
    {
        return new(
            MatchmakingServiceCallStatus.BadRequest,
            null,
            errorCode,
            errorMessage);
    }

    public static MatchmakingServiceCallResult<T> Conflict(
        string errorCode,
        string errorMessage)
    {
        return new(
            MatchmakingServiceCallStatus.Conflict,
            null,
            errorCode,
            errorMessage);
    }

    public static MatchmakingServiceCallResult<T> Unauthorized(
        string errorCode,
        string errorMessage)
    {
        return new(
            MatchmakingServiceCallStatus.Unauthorized,
            null,
            errorCode,
            errorMessage);
    }

    public static MatchmakingServiceCallResult<T> Unavailable(
        string errorCode,
        string errorMessage)
    {
        return new(
            MatchmakingServiceCallStatus.Unavailable,
            null,
            errorCode,
            errorMessage);
    }

    public static MatchmakingServiceCallResult<T> InvalidResponse(
        string errorCode,
        string errorMessage)
    {
        return new(
            MatchmakingServiceCallStatus.InvalidResponse,
            null,
            errorCode,
            errorMessage);
    }
}