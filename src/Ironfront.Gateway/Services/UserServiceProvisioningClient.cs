using System.Net.Http.Json;
using Ironfront.Shared.Contracts.UserService;
using Ironfront.Shared.Constants;

namespace Ironfront.Gateway.Services;

public sealed class UserServiceProvisioningClient
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly ILogger<UserServiceProvisioningClient> logger;

    public UserServiceProvisioningClient(
        IHttpClientFactory httpClientFactory,
        ILogger<UserServiceProvisioningClient> logger)
    {
        this.httpClientFactory = httpClientFactory;
        this.logger = logger;
    }

    public async Task<UserProvisioningResult> EnsureProvisionedAsync(
        Guid userId,
        string username,
        string email,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            return UserProvisioningResult.Fail(
                "invalid_user_identity",
                "User identity did not contain a valid user ID.");
        }

        if (string.IsNullOrWhiteSpace(username)
            || string.IsNullOrWhiteSpace(email))
        {
            return UserProvisioningResult.Fail(
                "invalid_user_identity",
                "User identity did not contain a valid username or email.");
        }

        var request = new ProvisionUserRequest(
            userId,
            username,
            email);

        try
        {
            HttpClient userServiceClient =
            httpClientFactory.CreateClient(ServiceNames.UserService);

            using HttpResponseMessage response =
            await userServiceClient.PostAsJsonAsync(
                "/internal/users/provision",
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "UserService provisioning failed for user {UserId}. HTTP status: {StatusCode}.",
                    userId,
                    (int)response.StatusCode);

                return UserProvisioningResult.Fail(
                    "user_service_provisioning_failed",
                    "The game profile could not be initialized.");
            }

            ProvisionUserResponse? profile =
            await response.Content.ReadFromJsonAsync<ProvisionUserResponse>(
                cancellationToken: cancellationToken);

            if (profile is null
                || profile.UserId != userId
                || profile.ActiveDeckId == Guid.Empty)
            {
                logger.LogWarning(
                    "UserService returned an invalid provisioning response for user {UserId}.",
                    userId);

                return UserProvisioningResult.Fail(
                    "user_service_invalid_response",
                    "The game profile service returned an invalid response.");
            }

            return UserProvisioningResult.Ok(profile);
        }
        catch (OperationCanceledException)
        when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(
                exception,
                "UserService was unreachable while provisioning user {UserId}.",
                userId);

            return UserProvisioningResult.Fail(
                "user_service_unreachable",
                "The game profile service is currently unavailable.");
        }
    }
}

public sealed record UserProvisioningResult(
    bool Success,
    ProvisionUserResponse? Profile,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static UserProvisioningResult Ok(
        ProvisionUserResponse profile)
    {
        return new UserProvisioningResult(
            true,
            profile,
            null,
            null);
    }

    public static UserProvisioningResult Fail(
        string errorCode,
        string errorMessage)
    {
        return new UserProvisioningResult(
            false,
            null,
            errorCode,
            errorMessage);
    }
}
