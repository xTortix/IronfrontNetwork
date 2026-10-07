using System.Net;
using System.Net.Http.Json;
using Ironfront.Shared.Constants;
using Ironfront.Shared.Contracts.UserService;

namespace Ironfront.Gateway.Services;

public sealed class UserServiceHangarClient
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly ILogger<UserServiceHangarClient> logger;

    public UserServiceHangarClient(
        IHttpClientFactory httpClientFactory,
        ILogger<UserServiceHangarClient> logger)
    {
        this.httpClientFactory = httpClientFactory;
        this.logger = logger;
    }

    public async Task<UserServiceHangarFetchResult> GetHangarAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            return UserServiceHangarFetchResult.InvalidResponse(
                "invalid_user_identity",
                "The access token did not contain a valid user ID.");
        }

        try
        {
            HttpClient userServiceClient =
            httpClientFactory.CreateClient(ServiceNames.UserService);

            using HttpResponseMessage response =
            await userServiceClient.GetAsync(
                $"/internal/users/{userId:D}/hangar",
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return UserServiceHangarFetchResult.NotFound(
                    "hangar_not_found",
                    "No game profile exists for this account.");
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "UserService returned HTTP {StatusCode} while loading hangar for user {UserId}.",
                    (int)response.StatusCode,
                                  userId);

                return UserServiceHangarFetchResult.Unavailable(
                    "user_service_unavailable",
                    "The player data service is currently unavailable.");
            }

            UserHangarSnapshotResponse? hangar =
            await response.Content.ReadFromJsonAsync<UserHangarSnapshotResponse>(
                cancellationToken: cancellationToken);

            if (hangar is null
                || hangar.UserId != userId
                || hangar.ActiveDeckId == Guid.Empty
                || hangar.OwnedVehicles is null
                || hangar.Decks is null)
            {
                logger.LogWarning(
                    "UserService returned an invalid hangar response for user {UserId}.",
                    userId);

                return UserServiceHangarFetchResult.InvalidResponse(
                    "user_service_invalid_response",
                    "The player data service returned invalid hangar data.");
            }

            return UserServiceHangarFetchResult.Success(hangar);
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
                "UserService timed out while loading hangar for user {UserId}.",
                userId);

            return UserServiceHangarFetchResult.Unavailable(
                "user_service_timeout",
                "The player data service took too long to respond.");
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(
                exception,
                "UserService was unreachable while loading hangar for user {UserId}.",
                userId);

            return UserServiceHangarFetchResult.Unavailable(
                "user_service_unreachable",
                "The player data service is currently unavailable.");
        }
    }
}

public enum UserServiceHangarFetchStatus : byte
{
    Success = 0,
    NotFound = 1,
    Unavailable = 2,
    InvalidResponse = 3
}

public sealed record UserServiceHangarFetchResult(
    UserServiceHangarFetchStatus Status,
    UserHangarSnapshotResponse? Hangar,
    string ErrorCode,
    string ErrorMessage)
{
    public static UserServiceHangarFetchResult Success(
        UserHangarSnapshotResponse hangar)
    {
        return new(
            UserServiceHangarFetchStatus.Success,
            hangar,
            string.Empty,
            string.Empty);
    }

    public static UserServiceHangarFetchResult NotFound(
        string errorCode,
        string errorMessage)
    {
        return new(
            UserServiceHangarFetchStatus.NotFound,
            null,
            errorCode,
            errorMessage);
    }

    public static UserServiceHangarFetchResult Unavailable(
        string errorCode,
        string errorMessage)
    {
        return new(
            UserServiceHangarFetchStatus.Unavailable,
            null,
            errorCode,
            errorMessage);
    }

    public static UserServiceHangarFetchResult InvalidResponse(
        string errorCode,
        string errorMessage)
    {
        return new(
            UserServiceHangarFetchStatus.InvalidResponse,
            null,
            errorCode,
            errorMessage);
    }
}
