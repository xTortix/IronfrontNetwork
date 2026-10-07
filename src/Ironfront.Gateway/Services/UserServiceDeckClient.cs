using System.Net;
using Ironfront.Shared.Contracts.UserService;
using Ironfront.Shared.Constants;



namespace Ironfront.Gateway.Services;

public sealed class UserServiceDeckClient
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly ILogger<UserServiceDeckClient> logger;

    public UserServiceDeckClient(
        IHttpClientFactory httpClientFactory,
        ILogger<UserServiceDeckClient> logger)
    {
        this.httpClientFactory = httpClientFactory;
        this.logger = logger;
    }

    public async Task<UserServiceDeckCreateResult> CreateDeckAsync(
        Guid userId,
        CreateUserDeckRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            HttpClient client = httpClientFactory.CreateClient(
                ServiceNames.UserService);

            using HttpResponseMessage response =
                await client.PostAsJsonAsync(
                    $"/internal/users/{userId}/decks",
                    request,
                    cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return UserServiceDeckCreateResult.NotFound(
                    "user_profile_not_found",
                    "No user profile exists for this account.");
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                return UserServiceDeckCreateResult.InvalidRequest(
                    "invalid_deck_request",
                    "The deck request was rejected by the user service.");
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "UserService returned status code {StatusCode} while creating a deck for user {UserId}.",
                    (int)response.StatusCode,
                    userId);

                return UserServiceDeckCreateResult.Unavailable(
                    "user_service_unavailable",
                    "The user data service is currently unavailable.");
            }

            CreateUserDeckResponse? createdDeck =
                await response.Content.ReadFromJsonAsync<
                    CreateUserDeckResponse>(
                    cancellationToken: cancellationToken);

            if (createdDeck is null
                || createdDeck.UserId != userId
                || createdDeck.DeckId == Guid.Empty)
            {
                logger.LogError(
                    "UserService returned an invalid deck creation response for user {UserId}.",
                    userId);

                return UserServiceDeckCreateResult.InvalidResponse(
                    "invalid_user_service_response",
                    "The user data service returned invalid deck data.");
            }

            return UserServiceDeckCreateResult.Success(createdDeck);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(
                exception,
                "Could not reach UserService while creating a deck for user {UserId}.",
                userId);

            return UserServiceDeckCreateResult.Unavailable(
                "user_service_unavailable",
                "The user data service is currently unavailable.");
        }
        catch (TaskCanceledException exception)
            when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                exception,
                "UserService timed out while creating a deck for user {UserId}.",
                userId);

            return UserServiceDeckCreateResult.Unavailable(
                "user_service_timeout",
                "The user data service did not respond in time.");
        }
    }
   public async Task<UserServiceDeckActivationResult> ActivateDeckAsync(
    Guid userId,
    Guid deckId,
    CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User ID must not be empty.",
                nameof(userId));
        }

        if (deckId == Guid.Empty)
        {
            throw new ArgumentException(
                "Deck ID must not be empty.",
                nameof(deckId));
        }

        try
        {
            HttpClient client = httpClientFactory.CreateClient(
                ServiceNames.UserService);

            using HttpResponseMessage response =
                await client.PutAsync(
                    $"/internal/users/{userId}/decks/{deckId}/active",
                    null,
                    cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return UserServiceDeckActivationResult.NotFound(
                    "user_deck_not_found",
                    "The requested deck does not exist for this account.");
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                return UserServiceDeckActivationResult.InvalidRequest(
                    "invalid_deck_activation_request",
                    "The deck activation request was rejected by the user service.");
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "UserService returned status code {StatusCode} while activating deck {DeckId} for user {UserId}.",
                    (int)response.StatusCode,
                    deckId,
                    userId);

                return UserServiceDeckActivationResult.Unavailable(
                    "user_service_unavailable",
                    "The user data service is currently unavailable.");
            }

            UserDeckActivationResponse? activatedDeck =
                await response.Content.ReadFromJsonAsync<
                    UserDeckActivationResponse>(
                    cancellationToken: cancellationToken);

            if (activatedDeck is null
                || activatedDeck.UserId != userId
                || activatedDeck.DeckId != deckId
                || !activatedDeck.IsActive)
            {
                logger.LogError(
                    "UserService returned an invalid deck activation response for deck {DeckId} and user {UserId}.",
                    deckId,
                    userId);

                return UserServiceDeckActivationResult.InvalidResponse(
                    "invalid_user_service_response",
                    "The user data service returned invalid deck activation data.");
            }

            return UserServiceDeckActivationResult.Success(
                activatedDeck);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(
                exception,
                "Could not reach UserService while activating deck {DeckId} for user {UserId}.",
                deckId,
                userId);

            return UserServiceDeckActivationResult.Unavailable(
                "user_service_unavailable",
                "The user data service is currently unavailable.");
        }
        catch (TaskCanceledException exception)
            when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                exception,
                "UserService timed out while activating deck {DeckId} for user {UserId}.",
                deckId,
                userId);

            return UserServiceDeckActivationResult.Unavailable(
                "user_service_timeout",
                "The user data service did not respond in time.");
        }
    } 
   
   public async Task<UserServiceDeckSlotAssignmentResult>
    AssignVehicleToDeckSlotAsync(
        Guid userId,
        Guid deckId,
        int slotIndex,
        AssignUserDeckSlotVehicleRequest request,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User ID must not be empty.",
                nameof(userId));
        }

        if (deckId == Guid.Empty)
        {
            throw new ArgumentException(
                "Deck ID must not be empty.",
                nameof(deckId));
        }

        ArgumentNullException.ThrowIfNull(request);

        try
        {
            HttpClient client = httpClientFactory.CreateClient(
                ServiceNames.UserService);

            using HttpResponseMessage response =
                await client.PutAsJsonAsync(
                    $"/internal/users/{userId}/decks/{deckId}/slots/{slotIndex}",
                    request,
                    cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return UserServiceDeckSlotAssignmentResult.NotFound(
                    "user_deck_not_found",
                    "The requested deck does not exist for this account.");
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                return UserServiceDeckSlotAssignmentResult.Forbidden(
                    "user_vehicle_not_owned",
                    "The requested vehicle is not owned by this account.");
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                return UserServiceDeckSlotAssignmentResult.Conflict(
                    "deck_slot_assignment_conflict",
                    "The vehicle cannot be assigned to the requested deck slot.");
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                return UserServiceDeckSlotAssignmentResult.InvalidRequest(
                    "invalid_deck_slot_assignment_request",
                    "The deck slot assignment request was rejected by the user service.");
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "UserService returned status code {StatusCode} while assigning a vehicle to slot {SlotIndex} of deck {DeckId} for user {UserId}.",
                    (int)response.StatusCode,
                    slotIndex,
                    deckId,
                    userId);

                return UserServiceDeckSlotAssignmentResult.Unavailable(
                    "user_service_unavailable",
                    "The user data service is currently unavailable.");
            }

            UserDeckSlotAssignmentResponse? assignment =
                await response.Content.ReadFromJsonAsync<
                    UserDeckSlotAssignmentResponse>(
                    cancellationToken: cancellationToken);

            if (assignment is null
                || assignment.UserId != userId
                || assignment.DeckId != deckId
                || assignment.SlotIndex != slotIndex
                || string.IsNullOrWhiteSpace(assignment.VehicleId))
            {
                logger.LogError(
                    "UserService returned an invalid deck slot assignment response for deck {DeckId}, slot {SlotIndex}, and user {UserId}.",
                    deckId,
                    slotIndex,
                    userId);

                return UserServiceDeckSlotAssignmentResult.InvalidResponse(
                    "invalid_user_service_response",
                    "The user data service returned invalid deck slot assignment data.");
            }

            return UserServiceDeckSlotAssignmentResult.Success(
                assignment);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(
                exception,
                "Could not reach UserService while assigning a vehicle to slot {SlotIndex} of deck {DeckId} for user {UserId}.",
                slotIndex,
                deckId,
                userId);

            return UserServiceDeckSlotAssignmentResult.Unavailable(
                "user_service_unavailable",
                "The user data service is currently unavailable.");
        }
        catch (TaskCanceledException exception)
            when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                exception,
                "UserService timed out while assigning a vehicle to slot {SlotIndex} of deck {DeckId} for user {UserId}.",
                slotIndex,
                deckId,
                userId);

            return UserServiceDeckSlotAssignmentResult.Unavailable(
                "user_service_timeout",
                "The user data service did not respond in time.");
        }
    }
   
   public async Task<UserServiceDeckSlotClearResult> ClearDeckSlotAsync(
    Guid userId,
    Guid deckId,
    int slotIndex,
    CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User ID must not be empty.",
                nameof(userId));
        }

        if (deckId == Guid.Empty)
        {
            throw new ArgumentException(
                "Deck ID must not be empty.",
                nameof(deckId));
        }

        try
        {
            HttpClient client = httpClientFactory.CreateClient(
                ServiceNames.UserService);

            using HttpResponseMessage response =
                await client.DeleteAsync(
                    $"/internal/users/{userId}/decks/{deckId}/slots/{slotIndex}",
                    cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return UserServiceDeckSlotClearResult.NotFound(
                    "user_deck_not_found",
                    "The requested deck does not exist for this account.");
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                return UserServiceDeckSlotClearResult.InvalidRequest(
                    "invalid_deck_slot_clear_request",
                    "The deck slot clear request was rejected by the user service.");
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "UserService returned status code {StatusCode} while clearing slot {SlotIndex} of deck {DeckId} for user {UserId}.",
                    (int)response.StatusCode,
                    slotIndex,
                    deckId,
                    userId);

                return UserServiceDeckSlotClearResult.Unavailable(
                    "user_service_unavailable",
                    "The user data service is currently unavailable.");
            }

            UserDeckSlotClearResponse? clearResponse =
                await response.Content.ReadFromJsonAsync<
                    UserDeckSlotClearResponse>(
                    cancellationToken: cancellationToken);

            if (clearResponse is null
                || clearResponse.UserId != userId
                || clearResponse.DeckId != deckId
                || clearResponse.SlotIndex != slotIndex
                || (clearResponse.WasCleared
                    && clearResponse.ClearedAtUtc is null)
                || (!clearResponse.WasCleared
                    && clearResponse.ClearedAtUtc is not null))
            {
                logger.LogError(
                    "UserService returned an invalid deck slot clear response for deck {DeckId}, slot {SlotIndex}, and user {UserId}.",
                    deckId,
                    slotIndex,
                    userId);

                return UserServiceDeckSlotClearResult.InvalidResponse(
                    "invalid_user_service_response",
                    "The user data service returned invalid deck slot clear data.");
            }

            return UserServiceDeckSlotClearResult.Success(
                clearResponse);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(
                exception,
                "Could not reach UserService while clearing slot {SlotIndex} of deck {DeckId} for user {UserId}.",
                slotIndex,
                deckId,
                userId);

            return UserServiceDeckSlotClearResult.Unavailable(
                "user_service_unavailable",
                "The user data service is currently unavailable.");
        }
        catch (TaskCanceledException exception)
            when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                exception,
                "UserService timed out while clearing slot {SlotIndex} of deck {DeckId} for user {UserId}.",
                slotIndex,
                deckId,
                userId);

            return UserServiceDeckSlotClearResult.Unavailable(
                "user_service_timeout",
                "The user data service did not respond in time.");
        }
    }
}

public enum UserServiceDeckCreateStatus : byte
{
    Success = 0,
    NotFound = 1,
    InvalidRequest = 2,
    Unavailable = 3,
    InvalidResponse = 4
}

public sealed record UserServiceDeckCreateResult(
    UserServiceDeckCreateStatus Status,
    CreateUserDeckResponse? CreatedDeck,
    string ErrorCode,
    string ErrorMessage)
{
    public static UserServiceDeckCreateResult Success(
        CreateUserDeckResponse createdDeck)
    {
        ArgumentNullException.ThrowIfNull(createdDeck);

        return new UserServiceDeckCreateResult(
            UserServiceDeckCreateStatus.Success,
            createdDeck,
            string.Empty,
            string.Empty);
    }

    public static UserServiceDeckCreateResult NotFound(
        string errorCode,
        string errorMessage)
    {
        return new UserServiceDeckCreateResult(
            UserServiceDeckCreateStatus.NotFound,
            null,
            errorCode,
            errorMessage);
    }

    public static UserServiceDeckCreateResult InvalidRequest(
        string errorCode,
        string errorMessage)
    {
        return new UserServiceDeckCreateResult(
            UserServiceDeckCreateStatus.InvalidRequest,
            null,
            errorCode,
            errorMessage);
    }

    public static UserServiceDeckCreateResult Unavailable(
        string errorCode,
        string errorMessage)
    {
        return new UserServiceDeckCreateResult(
            UserServiceDeckCreateStatus.Unavailable,
            null,
            errorCode,
            errorMessage);
    }

    public static UserServiceDeckCreateResult InvalidResponse(
        string errorCode,
        string errorMessage)
    {
        return new UserServiceDeckCreateResult(
            UserServiceDeckCreateStatus.InvalidResponse,
            null,
            errorCode,
            errorMessage);
    }
}

public enum UserServiceDeckActivationStatus : byte
{
    Success = 0,
    NotFound = 1,
    InvalidRequest = 2,
    Unavailable = 3,
    InvalidResponse = 4
}

public sealed record UserServiceDeckActivationResult(
    UserServiceDeckActivationStatus Status,
    UserDeckActivationResponse? ActivatedDeck,
    string ErrorCode,
    string ErrorMessage)
{
    public static UserServiceDeckActivationResult Success(
        UserDeckActivationResponse activatedDeck)
    {
        ArgumentNullException.ThrowIfNull(activatedDeck);

        return new UserServiceDeckActivationResult(
            UserServiceDeckActivationStatus.Success,
            activatedDeck,
            string.Empty,
            string.Empty);
    }

    public static UserServiceDeckActivationResult NotFound(
        string errorCode,
        string errorMessage)
    {
        return new UserServiceDeckActivationResult(
            UserServiceDeckActivationStatus.NotFound,
            null,
            errorCode,
            errorMessage);
    }

    public static UserServiceDeckActivationResult InvalidRequest(
        string errorCode,
        string errorMessage)
    {
        return new UserServiceDeckActivationResult(
            UserServiceDeckActivationStatus.InvalidRequest,
            null,
            errorCode,
            errorMessage);
    }

    public static UserServiceDeckActivationResult Unavailable(
        string errorCode,
        string errorMessage)
    {
        return new UserServiceDeckActivationResult(
            UserServiceDeckActivationStatus.Unavailable,
            null,
            errorCode,
            errorMessage);
    }

    public static UserServiceDeckActivationResult InvalidResponse(
        string errorCode,
        string errorMessage)
    {
        return new UserServiceDeckActivationResult(
            UserServiceDeckActivationStatus.InvalidResponse,
            null,
            errorCode,
            errorMessage);
    }
}

public enum UserServiceDeckSlotAssignmentStatus : byte
{
    Success = 0,
    NotFound = 1,
    Forbidden = 2,
    Conflict = 3,
    InvalidRequest = 4,
    Unavailable = 5,
    InvalidResponse = 6
}

public sealed record UserServiceDeckSlotAssignmentResult(
    UserServiceDeckSlotAssignmentStatus Status,
    UserDeckSlotAssignmentResponse? Assignment,
    string ErrorCode,
    string ErrorMessage)
{
    public static UserServiceDeckSlotAssignmentResult Success(
        UserDeckSlotAssignmentResponse assignment)
    {
        ArgumentNullException.ThrowIfNull(assignment);

        return new UserServiceDeckSlotAssignmentResult(
            UserServiceDeckSlotAssignmentStatus.Success,
            assignment,
            string.Empty,
            string.Empty);
    }

    public static UserServiceDeckSlotAssignmentResult NotFound(
        string errorCode,
        string errorMessage)
    {
        return new UserServiceDeckSlotAssignmentResult(
            UserServiceDeckSlotAssignmentStatus.NotFound,
            null,
            errorCode,
            errorMessage);
    }

    public static UserServiceDeckSlotAssignmentResult Forbidden(
        string errorCode,
        string errorMessage)
    {
        return new UserServiceDeckSlotAssignmentResult(
            UserServiceDeckSlotAssignmentStatus.Forbidden,
            null,
            errorCode,
            errorMessage);
    }

    public static UserServiceDeckSlotAssignmentResult Conflict(
        string errorCode,
        string errorMessage)
    {
        return new UserServiceDeckSlotAssignmentResult(
            UserServiceDeckSlotAssignmentStatus.Conflict,
            null,
            errorCode,
            errorMessage);
    }

    public static UserServiceDeckSlotAssignmentResult InvalidRequest(
        string errorCode,
        string errorMessage)
    {
        return new UserServiceDeckSlotAssignmentResult(
            UserServiceDeckSlotAssignmentStatus.InvalidRequest,
            null,
            errorCode,
            errorMessage);
    }

    public static UserServiceDeckSlotAssignmentResult Unavailable(
        string errorCode,
        string errorMessage)
    {
        return new UserServiceDeckSlotAssignmentResult(
            UserServiceDeckSlotAssignmentStatus.Unavailable,
            null,
            errorCode,
            errorMessage);
    }

    public static UserServiceDeckSlotAssignmentResult InvalidResponse(
        string errorCode,
        string errorMessage)
    {
        return new UserServiceDeckSlotAssignmentResult(
            UserServiceDeckSlotAssignmentStatus.InvalidResponse,
            null,
            errorCode,
            errorMessage);
    }
}


public enum UserServiceDeckSlotClearStatus : byte
{
    Success = 0,
    NotFound = 1,
    InvalidRequest = 2,
    Unavailable = 3,
    InvalidResponse = 4
}

public sealed record UserServiceDeckSlotClearResult(
    UserServiceDeckSlotClearStatus Status,
    UserDeckSlotClearResponse? ClearResponse,
    string ErrorCode,
    string ErrorMessage)
{
    public static UserServiceDeckSlotClearResult Success(
        UserDeckSlotClearResponse clearResponse)
    {
        ArgumentNullException.ThrowIfNull(clearResponse);

        return new UserServiceDeckSlotClearResult(
            UserServiceDeckSlotClearStatus.Success,
            clearResponse,
            string.Empty,
            string.Empty);
    }

    public static UserServiceDeckSlotClearResult NotFound(
        string errorCode,
        string errorMessage)
    {
        return new UserServiceDeckSlotClearResult(
            UserServiceDeckSlotClearStatus.NotFound,
            null,
            errorCode,
            errorMessage);
    }

    public static UserServiceDeckSlotClearResult InvalidRequest(
        string errorCode,
        string errorMessage)
    {
        return new UserServiceDeckSlotClearResult(
            UserServiceDeckSlotClearStatus.InvalidRequest,
            null,
            errorCode,
            errorMessage);
    }

    public static UserServiceDeckSlotClearResult Unavailable(
        string errorCode,
        string errorMessage)
    {
        return new UserServiceDeckSlotClearResult(
            UserServiceDeckSlotClearStatus.Unavailable,
            null,
            errorCode,
            errorMessage);
    }

    public static UserServiceDeckSlotClearResult InvalidResponse(
        string errorCode,
        string errorMessage)
    {
        return new UserServiceDeckSlotClearResult(
            UserServiceDeckSlotClearStatus.InvalidResponse,
            null,
            errorCode,
            errorMessage);
    }
}