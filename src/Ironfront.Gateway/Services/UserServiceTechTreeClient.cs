using System.Net;
using System.Net.Http.Json;
using Ironfront.Shared.Constants;
using Ironfront.Shared.Contracts.UserService;
using Ironfront.Shared.Contracts.TechTree;


namespace Ironfront.Gateway.Services;

public sealed class UserServiceTechTreeClient
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly ILogger<UserServiceTechTreeClient> logger;

    public UserServiceTechTreeClient(
        IHttpClientFactory httpClientFactory,
        ILogger<UserServiceTechTreeClient> logger)
    {
        this.httpClientFactory = httpClientFactory;
        this.logger = logger;
    }

    public async Task<UserServiceTechTreeProgressFetchResult>
        GetProgressAsync(
            Guid userId,
            string techTreeId,
            CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            return UserServiceTechTreeProgressFetchResult.InvalidResponse(
                "invalid_user_identity",
                "The access token did not contain a valid user ID.");
        }

        if (string.IsNullOrWhiteSpace(techTreeId))
        {
            return UserServiceTechTreeProgressFetchResult.InvalidRequest(
                "invalid_tech_tree_id",
                "A tech tree ID is required.");
        }

        string normalizedTechTreeId = techTreeId.Trim();

        try
        {
            HttpClient userServiceClient =
                httpClientFactory.CreateClient(
                    ServiceNames.UserService);

            string encodedTechTreeId =
                Uri.EscapeDataString(normalizedTechTreeId);

            using HttpResponseMessage response =
                await userServiceClient.GetAsync(
                    $"/internal/users/{userId:D}/tech-trees/{encodedTechTreeId}/progress",
                    cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return UserServiceTechTreeProgressFetchResult.NotFound(
                    "tech_tree_progress_not_found",
                    "The requested tech tree or player profile could not be found.");
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                return UserServiceTechTreeProgressFetchResult.InvalidRequest(
                    "invalid_tech_tree_progress_request",
                    "The requested tech tree progress could not be loaded.");
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "UserService returned HTTP {StatusCode} while loading tech tree {TechTreeId} for user {UserId}.",
                    (int)response.StatusCode,
                    normalizedTechTreeId,
                    userId);

                return UserServiceTechTreeProgressFetchResult.Unavailable(
                    "user_service_unavailable",
                    "The player data service is currently unavailable.");
            }

            UserTechTreeProgressResponse? progress =
                await response.Content.ReadFromJsonAsync<
                    UserTechTreeProgressResponse>(
                    cancellationToken: cancellationToken);

            if (progress is null
                || progress.UserId != userId
                || !string.Equals(
                    progress.TechTreeId,
                    normalizedTechTreeId,
                    StringComparison.OrdinalIgnoreCase)
                || progress.Nodes is null)
            {
                logger.LogWarning(
                    "UserService returned invalid tech tree progress for user {UserId} and tree {TechTreeId}.",
                    userId,
                    normalizedTechTreeId);

                return UserServiceTechTreeProgressFetchResult.InvalidResponse(
                    "user_service_invalid_response",
                    "The player data service returned invalid tech tree progress.");
            }

            return UserServiceTechTreeProgressFetchResult.Success(
                progress);
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
                "UserService timed out while loading tech tree {TechTreeId} for user {UserId}.",
                normalizedTechTreeId,
                userId);

            return UserServiceTechTreeProgressFetchResult.Unavailable(
                "user_service_timeout",
                "The player data service took too long to respond.");
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(
                exception,
                "UserService was unreachable while loading tech tree {TechTreeId} for user {UserId}.",
                normalizedTechTreeId,
                userId);

            return UserServiceTechTreeProgressFetchResult.Unavailable(
                "user_service_unreachable",
                "The player data service is currently unavailable.");
        }
    }
    
    public async Task<UserServiceResearchTargetSelectResult>
    SelectResearchTargetAsync(
        Guid userId,
        string techTreeId,
        string nodeId,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            return UserServiceResearchTargetSelectResult.InvalidRequest(
                "invalid_user_identity",
                "The access token did not contain a valid user ID.");
        }

        if (string.IsNullOrWhiteSpace(techTreeId))
        {
            return UserServiceResearchTargetSelectResult.InvalidRequest(
                "invalid_tech_tree_id",
                "A tech tree ID is required.");
        }

        if (string.IsNullOrWhiteSpace(nodeId))
        {
            return UserServiceResearchTargetSelectResult.InvalidRequest(
                "invalid_research_target",
                "A research node ID is required.");
        }

        string normalizedTechTreeId = techTreeId.Trim();
        string normalizedNodeId = nodeId.Trim();

        try
        {
            HttpClient userServiceClient =
                httpClientFactory.CreateClient(
                    ServiceNames.UserService);

            string encodedTechTreeId =
                Uri.EscapeDataString(normalizedTechTreeId);

            using HttpResponseMessage response =
                await userServiceClient.PutAsJsonAsync(
                    $"/internal/users/{userId:D}/tech-trees/" +
                    $"{encodedTechTreeId}/research-target",
                    new SelectResearchTargetRequest(
                        normalizedNodeId),
                    cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return UserServiceResearchTargetSelectResult.NotFound(
                    "research_target_not_found",
                    "The requested tech tree, node, or player profile could not be found.");
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                return UserServiceResearchTargetSelectResult.InvalidRequest(
                    "invalid_research_target",
                    "The requested research target is invalid.");
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                return UserServiceResearchTargetSelectResult.Conflict(
                    "research_target_unavailable",
                    "This vehicle cannot currently be selected as a research target.");
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "UserService returned HTTP {StatusCode} while selecting research target {NodeId} for user {UserId} in tech tree {TechTreeId}.",
                    (int)response.StatusCode,
                    normalizedNodeId,
                    userId,
                    normalizedTechTreeId);

                return UserServiceResearchTargetSelectResult.Unavailable(
                    "user_service_unavailable",
                    "The player data service is currently unavailable.");
            }

            UserTechTreeProgressResponse? progress =
                await response.Content.ReadFromJsonAsync<
                    UserTechTreeProgressResponse>(
                    cancellationToken: cancellationToken);

            if (progress is null
                || progress.UserId != userId
                || !string.Equals(
                    progress.TechTreeId,
                    normalizedTechTreeId,
                    StringComparison.OrdinalIgnoreCase)
                || progress.Nodes is null)
            {
                logger.LogWarning(
                    "UserService returned invalid progress after selecting research target {NodeId} for user {UserId} in tree {TechTreeId}.",
                    normalizedNodeId,
                    userId,
                    normalizedTechTreeId);

                return UserServiceResearchTargetSelectResult.InvalidResponse(
                    "user_service_invalid_response",
                    "The player data service returned invalid research progress.");
            }

            return UserServiceResearchTargetSelectResult.Success(
                progress);
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
                "UserService timed out while selecting research target {NodeId} for user {UserId} in tree {TechTreeId}.",
                normalizedNodeId,
                userId,
                normalizedTechTreeId);

            return UserServiceResearchTargetSelectResult.Unavailable(
                "user_service_timeout",
                "The player data service took too long to respond.");
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(
                exception,
                "UserService was unreachable while selecting research target {NodeId} for user {UserId} in tree {TechTreeId}.",
                normalizedNodeId,
                userId,
                normalizedTechTreeId);

            return UserServiceResearchTargetSelectResult.Unavailable(
                "user_service_unreachable",
                "The player data service is currently unavailable.");
        }
    }
}

public enum UserServiceTechTreeProgressFetchStatus : byte
{
    Success = 0,
    NotFound = 1,
    InvalidRequest = 2,
    Unavailable = 3,
    InvalidResponse = 4
}

public sealed record UserServiceTechTreeProgressFetchResult(
    UserServiceTechTreeProgressFetchStatus Status,
    UserTechTreeProgressResponse? Progress,
    string ErrorCode,
    string ErrorMessage)
{
    public static UserServiceTechTreeProgressFetchResult Success(
        UserTechTreeProgressResponse progress)
    {
        return new(
            UserServiceTechTreeProgressFetchStatus.Success,
            progress,
            string.Empty,
            string.Empty);
    }

    public static UserServiceTechTreeProgressFetchResult NotFound(
        string errorCode,
        string errorMessage)
    {
        return new(
            UserServiceTechTreeProgressFetchStatus.NotFound,
            null,
            errorCode,
            errorMessage);
    }

    public static UserServiceTechTreeProgressFetchResult InvalidRequest(
        string errorCode,
        string errorMessage)
    {
        return new(
            UserServiceTechTreeProgressFetchStatus.InvalidRequest,
            null,
            errorCode,
            errorMessage);
    }

    public static UserServiceTechTreeProgressFetchResult Unavailable(
        string errorCode,
        string errorMessage)
    {
        return new(
            UserServiceTechTreeProgressFetchStatus.Unavailable,
            null,
            errorCode,
            errorMessage);
    }

    public static UserServiceTechTreeProgressFetchResult InvalidResponse(
        string errorCode,
        string errorMessage)
    {
        return new(
            UserServiceTechTreeProgressFetchStatus.InvalidResponse,
            null,
            errorCode,
            errorMessage);
    }
}

public enum UserServiceResearchTargetSelectStatus : byte
{
    Success = 0,
    NotFound = 1,
    InvalidRequest = 2,
    Conflict = 3,
    Unavailable = 4,
    InvalidResponse = 5
}

public sealed record UserServiceResearchTargetSelectResult(
    UserServiceResearchTargetSelectStatus Status,
    UserTechTreeProgressResponse? Progress,
    string ErrorCode,
    string ErrorMessage)
{
    public static UserServiceResearchTargetSelectResult Success(
        UserTechTreeProgressResponse progress)
    {
        return new(
            UserServiceResearchTargetSelectStatus.Success,
            progress,
            string.Empty,
            string.Empty);
    }

    public static UserServiceResearchTargetSelectResult NotFound(
        string errorCode,
        string errorMessage)
    {
        return new(
            UserServiceResearchTargetSelectStatus.NotFound,
            null,
            errorCode,
            errorMessage);
    }

    public static UserServiceResearchTargetSelectResult InvalidRequest(
        string errorCode,
        string errorMessage)
    {
        return new(
            UserServiceResearchTargetSelectStatus.InvalidRequest,
            null,
            errorCode,
            errorMessage);
    }

    public static UserServiceResearchTargetSelectResult Conflict(
        string errorCode,
        string errorMessage)
    {
        return new(
            UserServiceResearchTargetSelectStatus.Conflict,
            null,
            errorCode,
            errorMessage);
    }

    public static UserServiceResearchTargetSelectResult Unavailable(
        string errorCode,
        string errorMessage)
    {
        return new(
            UserServiceResearchTargetSelectStatus.Unavailable,
            null,
            errorCode,
            errorMessage);
    }

    public static UserServiceResearchTargetSelectResult InvalidResponse(
        string errorCode,
        string errorMessage)
    {
        return new(
            UserServiceResearchTargetSelectStatus.InvalidResponse,
            null,
            errorCode,
            errorMessage);
    }
}