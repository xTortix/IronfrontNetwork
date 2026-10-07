using Ironfront.Shared.Constants;
using Ironfront.Shared.Contracts;
using Ironfront.Shared.Contracts.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Ironfront.GameData.Application;
using Ironfront.GameData.Infrastructure.Json;
using Ironfront.Shared.Contracts.GameData;
using Ironfront.Gateway.Services;
using Ironfront.Shared.Contracts.Hangar;
using Ironfront.Shared.Contracts.UserService;
using Ironfront.Shared.Contracts.TechTree;
using Ironfront.GameData.Domain;
using Ironfront.Shared.Contracts.Matchmaking;



var builder = WebApplication.CreateBuilder(args);

string jwtIssuer =
    Environment.GetEnvironmentVariable("IRONFRONT_JWT_ISSUER")
    ?? "Ironfront.AuthService";

string jwtAudience =
    Environment.GetEnvironmentVariable("IRONFRONT_JWT_AUDIENCE")
    ?? "Ironfront.Client";

string? jwtSigningKey =
    Environment.GetEnvironmentVariable("IRONFRONT_JWT_SIGNING_KEY");

if (string.IsNullOrWhiteSpace(jwtSigningKey))
{
    throw new InvalidOperationException(
        "Missing environment variable IRONFRONT_JWT_SIGNING_KEY.");
}

if (jwtSigningKey.Length < 32)
{
    throw new InvalidOperationException(
        "IRONFRONT_JWT_SIGNING_KEY must be at least 32 characters long.");
}

string? internalServiceKey =
    Environment.GetEnvironmentVariable(
        "IRONFRONT_INTERNAL_SERVICE_KEY");

if (string.IsNullOrWhiteSpace(internalServiceKey))
{
    throw new InvalidOperationException(
        "Missing environment variable IRONFRONT_INTERNAL_SERVICE_KEY.");
}

if (internalServiceKey.Length < 32)
{
    throw new InvalidOperationException(
        "IRONFRONT_INTERNAL_SERVICE_KEY must be at least 32 characters long.");
}

string userServiceBaseUrl =
    Environment.GetEnvironmentVariable(
        "IRONFRONT_USERSERVICE_URL")
    ?? "http://127.0.0.1:5020";

if (!Uri.TryCreate(
        userServiceBaseUrl,
        UriKind.Absolute,
        out Uri? userServiceBaseAddress))
{
    throw new InvalidOperationException(
        "IRONFRONT_USERSERVICE_URL must contain a valid absolute URL.");
}

string matchmakingServiceBaseUrl =
    Environment.GetEnvironmentVariable(
        "IRONFRONT_MATCHMAKING_URL")
    ?? "http://127.0.0.1:5030";

if (!Uri.TryCreate(
        matchmakingServiceBaseUrl,
        UriKind.Absolute,
        out Uri? matchmakingServiceBaseAddress))
{
    throw new InvalidOperationException(
        "IRONFRONT_MATCHMAKING_URL must contain " +
        "a valid absolute URL.");
}

builder.Services.AddHttpClient(
    ServiceNames.MatchmakingService,
    client =>
    {
        client.BaseAddress =
            matchmakingServiceBaseAddress;

        client.Timeout =
            TimeSpan.FromSeconds(5);

        client.DefaultRequestHeaders.Add(
            "X-Ironfront-Internal-Key",
            internalServiceKey);
    });

builder.Services.AddSingleton<
    MatchmakingServiceClient>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = true,
            ValidAudience = jwtAudience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSigningKey)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

#region GAME DATA
string gameDataDirectory =
    Environment.GetEnvironmentVariable("IRONFRONT_GAME_DATA_DIRECTORY")
    ?? Path.Combine(AppContext.BaseDirectory, "game-data");

string vehicleCatalogPath = Path.Combine(
    gameDataDirectory,
    "vehicles.catalog.json");

string techTreeCatalogPath = Path.Combine(
    gameDataDirectory,
    "tech-trees.catalog.json");

string battleModeCatalogPath = Path.Combine(
    gameDataDirectory,
    "battle-modes.catalog.json");

IVehicleCatalogLoader vehicleCatalogLoader =
    new JsonVehicleCatalogLoader();

ITechTreeCatalogLoader techTreeCatalogLoader =
    new JsonTechTreeCatalogLoader();

IBattleModeCatalogLoader battleModeCatalogLoader =
    new JsonBattleModeCatalogLoader();

IVehicleCatalog vehicleCatalog;
ITechTreeCatalog techTreeCatalog;
IBattleModeCatalog battleModeCatalog;

try
{
    vehicleCatalog = vehicleCatalogLoader.LoadFromFile(
        vehicleCatalogPath);

    techTreeCatalog = techTreeCatalogLoader.LoadFromFile(
        techTreeCatalogPath,
        vehicleCatalog);
    
    battleModeCatalog =
        battleModeCatalogLoader.LoadFromFile(
            battleModeCatalogPath);
}
catch (Exception exception)
{
    throw new InvalidOperationException(
        $"Could not start Gateway because game data could not be loaded from '{gameDataDirectory}'.",
        exception);
}

builder.Services.AddSingleton<IVehicleCatalog>(vehicleCatalog);
builder.Services.AddSingleton<ITechTreeCatalog>(techTreeCatalog);
builder.Services.AddSingleton<IBattleModeCatalog>(battleModeCatalog);
#endregion

builder.WebHost.ConfigureKestrel(options =>
{
    string hostingMode =
        Environment.GetEnvironmentVariable("IRONFRONT_GATEWAY_HOSTING_MODE")
        ?? "DirectHttps";

    if (string.Equals(hostingMode, "ReverseProxy", StringComparison.OrdinalIgnoreCase))
    {
        // Production behind Caddy/Nginx.
        // Only reachable from the same machine.
        options.ListenLocalhost(5000);
        return;
    }

    // Development / direct test mode.
    options.ListenAnyIP(5001, listenOptions =>
    {
        listenOptions.UseHttps();
    });
});

builder.Services.AddHttpClient(ServiceNames.AuthService, client =>
{
    string baseUrl =
        Environment.GetEnvironmentVariable("IRONFRONT_AUTHSERVICE_URL")
        ?? "http://localhost:5010";

    client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddHttpClient(ServiceNames.UserService, client =>
{
    client.BaseAddress = userServiceBaseAddress;
    client.Timeout = TimeSpan.FromSeconds(5);

    client.DefaultRequestHeaders.Add(
        "X-Ironfront-Internal-Key",
        internalServiceKey);
});

builder.Services.AddSingleton<UserServiceProvisioningClient>();
builder.Services.AddSingleton<UserServiceHangarClient>();
builder.Services.AddScoped<UserServiceDeckClient>();
builder.Services.AddSingleton<UserServiceTechTreeClient>();

var app = builder.Build();

app.Logger.LogInformation(
    "Loaded tech tree catalog version {CatalogVersion}, hash {CatalogHash}, tree count {TechTreeCount}, node count {NodeCount}.",
    techTreeCatalog.Manifest.Version,
    techTreeCatalog.Manifest.ContentHash,
    techTreeCatalog.Manifest.TechTreeCount,
    techTreeCatalog.Manifest.NodeCount);

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/health", () =>
{
    var status = new ServiceStatusDto
    {
        ServiceName = ServiceNames.Gateway,
        Status = "Online",
        ServerTimeUtc = DateTime.UtcNow,
        Environment = app.Environment.EnvironmentName
    };

    return Results.Ok(ApiResponse<ServiceStatusDto>.Ok(status));
});

app.MapGet("/api/system/status", async (IHttpClientFactory httpClientFactory) =>
{
    // Gateway
    var gatewayStatus = new ServiceStatusDto
    {
        ServiceName = ServiceNames.Gateway,
        Status = "Online",
        ServerTimeUtc = DateTime.UtcNow,
        Environment = app.Environment.EnvironmentName
    };
    
    // AuthService
    var authClient = httpClientFactory.CreateClient(ServiceNames.AuthService);
    object authServiceStatus;
    try
    {
        var authResponse = await authClient.GetFromJsonAsync<ApiResponse<ServiceStatusDto>>("/internal/health");

        authServiceStatus = new
        {
            serviceName = ServiceNames.AuthService,
            reachable = authResponse?.Success == true,
            status = authResponse?.Data?.Status ?? "Unknown",
            serverTimeUtc = authResponse?.Data?.ServerTimeUtc,
            environment = authResponse?.Data?.Environment
        };
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(
            ex,
            "AuthService health check failed.");

        authServiceStatus = new
        {
            serviceName = ServiceNames.AuthService,
            reachable = false,
            status = "Offline"
        };
    }
    
    // UserService
    var userClient = httpClientFactory.CreateClient(ServiceNames.UserService);
    object userServiceStatus;
    try
    {
        using HttpResponseMessage userResponse =
            await userClient.GetAsync("/internal/health");

        userServiceStatus = new
        {
            serviceName = ServiceNames.UserService,
            reachable = userResponse.IsSuccessStatusCode,
            status = userResponse.IsSuccessStatusCode
                ? "Online"
                : "Offline",
            statusCode = (int)userResponse.StatusCode
        };
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(
            ex,
            "UserService health check failed.");

        userServiceStatus = new
        {
            serviceName = ServiceNames.UserService,
            reachable = false,
            status = "Offline"
        };
    }
    
    var result = new
    {
        dependencies = new[]
        {
            authServiceStatus,
            userServiceStatus
        } 
    };
    
    return Results.Ok(ApiResponse<object>.Ok(result));
});

app.MapPost("/api/auth/register", async (
    RegisterRequest request,
    IHttpClientFactory httpClientFactory,
    UserServiceProvisioningClient userProvisioningClient,
    CancellationToken cancellationToken) =>
{
    var authClient = httpClientFactory.CreateClient(ServiceNames.AuthService);

    try
    {
        using HttpResponseMessage authResponse =
            await authClient.PostAsJsonAsync(
                "/internal/auth/register",
                request,
                cancellationToken);

        ApiResponse<RegisterResponse>? apiResponse =
            await authResponse.Content.ReadFromJsonAsync<
                ApiResponse<RegisterResponse>>(
                cancellationToken: cancellationToken);

        if (apiResponse is null)
        {
            return Results.Json(
                ApiResponse<RegisterResponse>.Fail(
                    "auth_service_invalid_response",
                    "AuthService returned an invalid response."),
                statusCode: StatusCodes.Status502BadGateway);
        }

        if (!authResponse.IsSuccessStatusCode
            || !apiResponse.Success
            || apiResponse.Data is null)
        {
            return Results.Json(
                apiResponse,
                statusCode: (int)authResponse.StatusCode);
        }

        RegisterResponse registeredUser = apiResponse.Data;

        UserProvisioningResult provisioningResult =
            await userProvisioningClient.EnsureProvisionedAsync(
                registeredUser.UserId,
                registeredUser.Username,
                registeredUser.Email,
                cancellationToken);

        if (!provisioningResult.Success)
        {
            // Der Auth-Account existiert bereits.
            // Ein späterer Login versucht das idempotente Provisioning erneut.
            return Results.Json(
                ApiResponse<RegisterResponse>.Fail(
                    "user_profile_provisioning_pending",
                    "Account was created, but the game profile could not be initialized. Please sign in again shortly."),
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        return Results.Json(
            apiResponse,
            statusCode: (int)authResponse.StatusCode);
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(
            ex,
            "AuthService request failed during registration.");

        return Results.Json(
            ApiResponse<RegisterResponse>.Fail(
                "auth_service_unreachable",
                "Authentication service is currently unavailable."),
            statusCode: StatusCodes.Status502BadGateway);
    }
});

app.MapPost("/api/auth/login", async (
    LoginRequest request,
    IHttpClientFactory httpClientFactory,
    UserServiceProvisioningClient userProvisioningClient,
    CancellationToken cancellationToken) =>
{
    var authClient = httpClientFactory.CreateClient(ServiceNames.AuthService);

    try
    {
        using HttpResponseMessage authResponse =
            await authClient.PostAsJsonAsync(
                "/internal/auth/login",
                request,
                cancellationToken);

        ApiResponse<LoginResponse>? apiResponse =
            await authResponse.Content.ReadFromJsonAsync<
                ApiResponse<LoginResponse>>(
                cancellationToken: cancellationToken);

        if (apiResponse is null)
        {
            return Results.Json(
                ApiResponse<LoginResponse>.Fail(
                    "auth_service_invalid_response",
                    "AuthService returned an invalid response."),
                statusCode: StatusCodes.Status502BadGateway);
        }

        if (!authResponse.IsSuccessStatusCode
            || !apiResponse.Success
            || apiResponse.Data is null)
        {
            return Results.Json(
                apiResponse,
                statusCode: (int)authResponse.StatusCode);
        }

        LoginResponse authenticatedUser = apiResponse.Data;

        UserProvisioningResult provisioningResult =
            await userProvisioningClient.EnsureProvisionedAsync(
                authenticatedUser.UserId,
                authenticatedUser.Username,
                authenticatedUser.Email,
                cancellationToken);

        if (!provisioningResult.Success)
        {
            return Results.Json(
                ApiResponse<LoginResponse>.Fail(
                    "user_profile_unavailable",
                    "Your game profile is currently unavailable. Please try again shortly."),
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        return Results.Json(
            apiResponse,
            statusCode: (int)authResponse.StatusCode);
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(
            ex,
            "AuthService request failed during login.");

        return Results.Json(
            ApiResponse<LoginResponse>.Fail(
                "auth_service_unreachable",
                "Authentication service is currently unavailable."),
            statusCode: StatusCodes.Status502BadGateway);
    }
});

app.MapGet("/api/me", (ClaimsPrincipal user) =>
{
    string? userId = user.FindFirstValue("user_id");
    string? username = user.FindFirstValue("username");
    string? email = user.FindFirstValue("email");

    var result = new
    {
        authenticated = user.Identity?.IsAuthenticated == true,
        userId,
        username,
        email
    };

    return Results.Ok(ApiResponse<object>.Ok(result));
})
.RequireAuthorization();

app.MapGet("/api/hangar", async (
        ClaimsPrincipal user,
        UserServiceHangarClient hangarClient,
        CancellationToken cancellationToken) =>
    {
        string? rawUserId = user.FindFirstValue("user_id");

        if (!Guid.TryParse(rawUserId, out Guid userId))
        {
            return Results.Json(
                ApiResponse<HangarSnapshotResponse>.Fail(
                    "invalid_access_token",
                    "The access token did not contain a valid user ID."),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        UserServiceHangarFetchResult hangarResult =
            await hangarClient.GetHangarAsync(
                userId,
                cancellationToken);

        if (hangarResult.Status == UserServiceHangarFetchStatus.NotFound)
        {
            return Results.Json(
                ApiResponse<HangarSnapshotResponse>.Fail(
                    hangarResult.ErrorCode,
                    hangarResult.ErrorMessage),
                statusCode: StatusCodes.Status404NotFound);
        }

        if (hangarResult.Status == UserServiceHangarFetchStatus.Unavailable)
        {
            return Results.Json(
                ApiResponse<HangarSnapshotResponse>.Fail(
                    hangarResult.ErrorCode,
                    hangarResult.ErrorMessage),
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        if (hangarResult.Status == UserServiceHangarFetchStatus.InvalidResponse
            || hangarResult.Hangar is null)
        {
            return Results.Json(
                ApiResponse<HangarSnapshotResponse>.Fail(
                    hangarResult.ErrorCode,
                    hangarResult.ErrorMessage),
                statusCode: StatusCodes.Status502BadGateway);
        }

        UserHangarSnapshotResponse internalHangar =
            hangarResult.Hangar;

        var response = new HangarSnapshotResponse(
            internalHangar.ActiveDeckId,
            internalHangar.OwnedVehicles,
            internalHangar.Decks);

        return Results.Ok(
            ApiResponse<HangarSnapshotResponse>.Ok(response));
    })
    .RequireAuthorization();

app.MapPost("/api/hangar/decks", async (
    ClaimsPrincipal user,
    CreateHangarDeckRequest request,
    UserServiceDeckClient deckClient,
    CancellationToken cancellationToken) =>
{
    string? rawUserId = user.FindFirstValue("user_id");

    if (!Guid.TryParse(rawUserId, out Guid userId))
    {
        return Results.Json(
            ApiResponse<HangarDeckCreationResponse>.Fail(
                "invalid_access_token",
                "The access token did not contain a valid user ID."),
            statusCode: StatusCodes.Status401Unauthorized);
    }

    UserServiceDeckCreateResult createResult =
        await deckClient.CreateDeckAsync(
            userId,
            new CreateUserDeckRequest(
                request.Name,
                request.Nation),
            cancellationToken);

    if (createResult.Status == UserServiceDeckCreateStatus.NotFound)
    {
        return Results.Json(
            ApiResponse<HangarDeckCreationResponse>.Fail(
                createResult.ErrorCode,
                createResult.ErrorMessage),
            statusCode: StatusCodes.Status404NotFound);
    }

    if (createResult.Status == UserServiceDeckCreateStatus.InvalidRequest)
    {
        return Results.Json(
            ApiResponse<HangarDeckCreationResponse>.Fail(
                createResult.ErrorCode,
                createResult.ErrorMessage),
            statusCode: StatusCodes.Status400BadRequest);
    }

    if (createResult.Status == UserServiceDeckCreateStatus.Unavailable)
    {
        return Results.Json(
            ApiResponse<HangarDeckCreationResponse>.Fail(
                createResult.ErrorCode,
                createResult.ErrorMessage),
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    if (createResult.Status == UserServiceDeckCreateStatus.InvalidResponse
        || createResult.CreatedDeck is null)
    {
        return Results.Json(
            ApiResponse<HangarDeckCreationResponse>.Fail(
                createResult.ErrorCode,
                createResult.ErrorMessage),
            statusCode: StatusCodes.Status502BadGateway);
    }

    CreateUserDeckResponse internalDeck =
        createResult.CreatedDeck;

    var response = new HangarDeckCreationResponse(
        internalDeck.DeckId,
        internalDeck.Name,
        internalDeck.Nation,
        internalDeck.IsActive,
        internalDeck.CreatedAtUtc,
        internalDeck.UpdatedAtUtc);

    return Results.Created(
        $"/api/hangar/decks/{internalDeck.DeckId:D}",
        ApiResponse<HangarDeckCreationResponse>.Ok(response));
})
.RequireAuthorization();

app.MapPut("/api/hangar/decks/{deckId:guid}/active", async (
    ClaimsPrincipal user,
    Guid deckId,
    UserServiceDeckClient deckClient,
    CancellationToken cancellationToken) =>
{
    string? rawUserId = user.FindFirstValue("user_id");

    if (!Guid.TryParse(rawUserId, out Guid userId))
    {
        return Results.Json(
            ApiResponse<HangarDeckActivationResponse>.Fail(
                "invalid_access_token",
                "The access token did not contain a valid user ID."),
            statusCode: StatusCodes.Status401Unauthorized);
    }

    UserServiceDeckActivationResult activationResult =
        await deckClient.ActivateDeckAsync(
            userId,
            deckId,
            cancellationToken);

    if (activationResult.Status
        == UserServiceDeckActivationStatus.NotFound)
    {
        return Results.Json(
            ApiResponse<HangarDeckActivationResponse>.Fail(
                activationResult.ErrorCode,
                activationResult.ErrorMessage),
            statusCode: StatusCodes.Status404NotFound);
    }

    if (activationResult.Status
        == UserServiceDeckActivationStatus.InvalidRequest)
    {
        return Results.Json(
            ApiResponse<HangarDeckActivationResponse>.Fail(
                activationResult.ErrorCode,
                activationResult.ErrorMessage),
            statusCode: StatusCodes.Status400BadRequest);
    }

    if (activationResult.Status
        == UserServiceDeckActivationStatus.Unavailable)
    {
        return Results.Json(
            ApiResponse<HangarDeckActivationResponse>.Fail(
                activationResult.ErrorCode,
                activationResult.ErrorMessage),
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    if (activationResult.Status
            == UserServiceDeckActivationStatus.InvalidResponse
        || activationResult.ActivatedDeck is null)
    {
        return Results.Json(
            ApiResponse<HangarDeckActivationResponse>.Fail(
                activationResult.ErrorCode,
                activationResult.ErrorMessage),
            statusCode: StatusCodes.Status502BadGateway);
    }

    UserDeckActivationResponse internalDeck =
        activationResult.ActivatedDeck;

    var response = new HangarDeckActivationResponse(
        internalDeck.DeckId,
        internalDeck.Name,
        internalDeck.Nation,
        internalDeck.IsActive,
        internalDeck.CreatedAtUtc,
        internalDeck.UpdatedAtUtc);

    return Results.Ok(
        ApiResponse<HangarDeckActivationResponse>.Ok(response));
})
.RequireAuthorization();

app.MapDelete(
    "/api/hangar/decks/{deckId:guid}/slots/{slotIndex:int}",
    async (
        ClaimsPrincipal user,
        Guid deckId,
        int slotIndex,
        UserServiceDeckClient deckClient,
        CancellationToken cancellationToken) =>
    {
        string? rawUserId = user.FindFirstValue("user_id");

        if (!Guid.TryParse(rawUserId, out Guid userId))
        {
            return Results.Json(
                ApiResponse<HangarDeckSlotClearResponse>.Fail(
                    "invalid_access_token",
                    "The access token did not contain a valid user ID."),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        UserServiceDeckSlotClearResult clearResult =
            await deckClient.ClearDeckSlotAsync(
                userId,
                deckId,
                slotIndex,
                cancellationToken);

        if (clearResult.Status
            == UserServiceDeckSlotClearStatus.NotFound)
        {
            return Results.Json(
                ApiResponse<HangarDeckSlotClearResponse>.Fail(
                    clearResult.ErrorCode,
                    clearResult.ErrorMessage),
                statusCode: StatusCodes.Status404NotFound);
        }

        if (clearResult.Status
            == UserServiceDeckSlotClearStatus.InvalidRequest)
        {
            return Results.Json(
                ApiResponse<HangarDeckSlotClearResponse>.Fail(
                    clearResult.ErrorCode,
                    clearResult.ErrorMessage),
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (clearResult.Status
            == UserServiceDeckSlotClearStatus.Unavailable)
        {
            return Results.Json(
                ApiResponse<HangarDeckSlotClearResponse>.Fail(
                    clearResult.ErrorCode,
                    clearResult.ErrorMessage),
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        if (clearResult.Status
                == UserServiceDeckSlotClearStatus.InvalidResponse
            || clearResult.ClearResponse is null)
        {
            return Results.Json(
                ApiResponse<HangarDeckSlotClearResponse>.Fail(
                    clearResult.ErrorCode,
                    clearResult.ErrorMessage),
                statusCode: StatusCodes.Status502BadGateway);
        }

        UserDeckSlotClearResponse internalClearResponse =
            clearResult.ClearResponse;

        var response = new HangarDeckSlotClearResponse(
            internalClearResponse.DeckId,
            internalClearResponse.SlotIndex,
            internalClearResponse.WasCleared,
            internalClearResponse.ClearedAtUtc);

        return Results.Ok(
            ApiResponse<HangarDeckSlotClearResponse>.Ok(response));
    })
.RequireAuthorization();

app.MapPut(
    "/api/hangar/decks/{deckId:guid}/slots/{slotIndex:int}",
    async (
        ClaimsPrincipal user,
        Guid deckId,
        int slotIndex,
        AssignVehicleToHangarDeckSlotRequest request,
        UserServiceDeckClient deckClient,
        CancellationToken cancellationToken) =>
    {
        string? rawUserId = user.FindFirstValue("user_id");

        if (!Guid.TryParse(rawUserId, out Guid userId))
        {
            return Results.Json(
                ApiResponse<HangarDeckSlotVehicleAssignmentResponse>.Fail(
                    "invalid_access_token",
                    "The access token did not contain a valid user ID."),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        UserServiceDeckSlotAssignmentResult assignmentResult =
            await deckClient.AssignVehicleToDeckSlotAsync(
                userId,
                deckId,
                slotIndex,
                new AssignUserDeckSlotVehicleRequest(
                    request.VehicleId),
                cancellationToken);

        if (assignmentResult.Status
            == UserServiceDeckSlotAssignmentStatus.NotFound)
        {
            return Results.Json(
                ApiResponse<HangarDeckSlotVehicleAssignmentResponse>.Fail(
                    assignmentResult.ErrorCode,
                    assignmentResult.ErrorMessage),
                statusCode: StatusCodes.Status404NotFound);
        }

        if (assignmentResult.Status
            == UserServiceDeckSlotAssignmentStatus.Forbidden)
        {
            return Results.Json(
                ApiResponse<HangarDeckSlotVehicleAssignmentResponse>.Fail(
                    assignmentResult.ErrorCode,
                    assignmentResult.ErrorMessage),
                statusCode: StatusCodes.Status403Forbidden);
        }

        if (assignmentResult.Status
            == UserServiceDeckSlotAssignmentStatus.Conflict)
        {
            return Results.Json(
                ApiResponse<HangarDeckSlotVehicleAssignmentResponse>.Fail(
                    assignmentResult.ErrorCode,
                    assignmentResult.ErrorMessage),
                statusCode: StatusCodes.Status409Conflict);
        }

        if (assignmentResult.Status
            == UserServiceDeckSlotAssignmentStatus.InvalidRequest)
        {
            return Results.Json(
                ApiResponse<HangarDeckSlotVehicleAssignmentResponse>.Fail(
                    assignmentResult.ErrorCode,
                    assignmentResult.ErrorMessage),
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (assignmentResult.Status
            == UserServiceDeckSlotAssignmentStatus.Unavailable)
        {
            return Results.Json(
                ApiResponse<HangarDeckSlotVehicleAssignmentResponse>.Fail(
                    assignmentResult.ErrorCode,
                    assignmentResult.ErrorMessage),
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        if (assignmentResult.Status
                == UserServiceDeckSlotAssignmentStatus.InvalidResponse
            || assignmentResult.Assignment is null)
        {
            return Results.Json(
                ApiResponse<HangarDeckSlotVehicleAssignmentResponse>.Fail(
                    assignmentResult.ErrorCode,
                    assignmentResult.ErrorMessage),
                statusCode: StatusCodes.Status502BadGateway);
        }

        UserDeckSlotAssignmentResponse internalAssignment =
            assignmentResult.Assignment;

        var response = new HangarDeckSlotVehicleAssignmentResponse(
            internalAssignment.DeckId,
            internalAssignment.SlotIndex,
            internalAssignment.VehicleId,
            internalAssignment.AddedAtUtc);

        return Results.Ok(
            ApiResponse<HangarDeckSlotVehicleAssignmentResponse>.Ok(
                response));
    })
.RequireAuthorization();

app.MapGet("/api/game-data/manifest", (IVehicleCatalog catalog) =>
{
    var response = new VehicleCatalogManifestResponse
    {
        Version = catalog.Manifest.Version,
        ContentHash = catalog.Manifest.ContentHash,
        VehicleCount = catalog.Manifest.VehicleCount
    };

    return Results.Ok(
        ApiResponse<VehicleCatalogManifestResponse>.Ok(response));
});

app.MapGet(
    "/api/tech-trees/{techTreeId}/progress",
    async (
        ClaimsPrincipal user,
        string techTreeId,
        UserServiceTechTreeClient techTreeClient,
        CancellationToken cancellationToken) =>
    {
        string? rawUserId = user.FindFirstValue("user_id");

        if (!Guid.TryParse(rawUserId, out Guid userId))
        {
            return Results.Json(
                ApiResponse<TechTreeProgressResponse>.Fail(
                    "invalid_access_token",
                    "The access token did not contain a valid user ID."),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        UserServiceTechTreeProgressFetchResult progressResult =
            await techTreeClient.GetProgressAsync(
                userId,
                techTreeId,
                cancellationToken);

        if (progressResult.Status
            == UserServiceTechTreeProgressFetchStatus.NotFound)
        {
            return Results.Json(
                ApiResponse<TechTreeProgressResponse>.Fail(
                    progressResult.ErrorCode,
                    progressResult.ErrorMessage),
                statusCode: StatusCodes.Status404NotFound);
        }

        if (progressResult.Status
            == UserServiceTechTreeProgressFetchStatus.InvalidRequest)
        {
            return Results.Json(
                ApiResponse<TechTreeProgressResponse>.Fail(
                    progressResult.ErrorCode,
                    progressResult.ErrorMessage),
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (progressResult.Status
            == UserServiceTechTreeProgressFetchStatus.Unavailable)
        {
            return Results.Json(
                ApiResponse<TechTreeProgressResponse>.Fail(
                    progressResult.ErrorCode,
                    progressResult.ErrorMessage),
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        if (progressResult.Status
                == UserServiceTechTreeProgressFetchStatus.InvalidResponse
            || progressResult.Progress is null)
        {
            return Results.Json(
                ApiResponse<TechTreeProgressResponse>.Fail(
                    progressResult.ErrorCode,
                    progressResult.ErrorMessage),
                statusCode: StatusCodes.Status502BadGateway);
        }

        UserTechTreeProgressResponse internalProgress =
            progressResult.Progress;

        var response = new TechTreeProgressResponse(
            internalProgress.TechTreeId,
            internalProgress.SelectedNodeId,
            internalProgress.Nodes
                .Select(ToTechTreeNodeProgressResponse)
                .ToArray());

        return Results.Ok(
            ApiResponse<TechTreeProgressResponse>.Ok(response));
    })
    .RequireAuthorization();

app.MapPut(
    "/api/tech-trees/{techTreeId}/research-target",
    async (
        ClaimsPrincipal user,
        string techTreeId,
        SelectResearchTargetRequest request,
        UserServiceTechTreeClient techTreeClient,
        CancellationToken cancellationToken) =>
    {
        string? rawUserId =
            user.FindFirstValue("user_id");

        if (!Guid.TryParse(rawUserId, out Guid userId))
        {
            return Results.Json(
                ApiResponse<TechTreeProgressResponse>.Fail(
                    "invalid_access_token",
                    "The access token did not contain a valid user ID."),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        if (request is null ||
            string.IsNullOrWhiteSpace(request.NodeId))
        {
            return Results.Json(
                ApiResponse<TechTreeProgressResponse>.Fail(
                    "invalid_research_target",
                    "A research node ID is required."),
                statusCode: StatusCodes.Status400BadRequest);
        }

        UserServiceResearchTargetSelectResult result =
            await techTreeClient.SelectResearchTargetAsync(
                userId,
                techTreeId,
                request.NodeId,
                cancellationToken);

        if (result.Status
            == UserServiceResearchTargetSelectStatus.NotFound)
        {
            return Results.Json(
                ApiResponse<TechTreeProgressResponse>.Fail(
                    result.ErrorCode,
                    result.ErrorMessage),
                statusCode: StatusCodes.Status404NotFound);
        }

        if (result.Status
            == UserServiceResearchTargetSelectStatus.InvalidRequest)
        {
            return Results.Json(
                ApiResponse<TechTreeProgressResponse>.Fail(
                    result.ErrorCode,
                    result.ErrorMessage),
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (result.Status
            == UserServiceResearchTargetSelectStatus.Conflict)
        {
            return Results.Json(
                ApiResponse<TechTreeProgressResponse>.Fail(
                    result.ErrorCode,
                    result.ErrorMessage),
                statusCode: StatusCodes.Status409Conflict);
        }

        if (result.Status
            == UserServiceResearchTargetSelectStatus.Unavailable)
        {
            return Results.Json(
                ApiResponse<TechTreeProgressResponse>.Fail(
                    result.ErrorCode,
                    result.ErrorMessage),
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        if (result.Status
                == UserServiceResearchTargetSelectStatus.InvalidResponse
            || result.Progress is null)
        {
            return Results.Json(
                ApiResponse<TechTreeProgressResponse>.Fail(
                    result.ErrorCode,
                    result.ErrorMessage),
                statusCode: StatusCodes.Status502BadGateway);
        }

        UserTechTreeProgressResponse internalProgress =
            result.Progress;

        var response = new TechTreeProgressResponse(
            internalProgress.TechTreeId,
            internalProgress.SelectedNodeId,
            internalProgress.Nodes
                .Select(ToTechTreeNodeProgressResponse)
                .ToArray());

        return Results.Ok(
            ApiResponse<TechTreeProgressResponse>.Ok(response));
    })
    .RequireAuthorization();

app.MapGet(
    "/api/matchmaking/modes",
    (IBattleModeCatalog battleModeCatalog) =>
    {
        DateTime utcNow = DateTime.UtcNow;

        IReadOnlyList<BattleModeResponse> modes =
            battleModeCatalog
                .GetAvailableModes(utcNow)
                .OrderBy(
                    mode => mode.DisplayName,
                    StringComparer.Ordinal)
                .Select(ToBattleModeResponse)
                .ToArray();

        return Results.Ok(
            ApiResponse<IReadOnlyList<BattleModeResponse>>
                .Ok(modes));
    })
    .RequireAuthorization();

app.MapPost(
    "/api/matchmaking/queue",
    async (
        ClaimsPrincipal user,
        JoinBattleQueueRequest request,
        UserServiceHangarClient hangarClient,
        MatchmakingServiceClient matchmakingClient,
        IVehicleCatalog vehicleCatalog,
        IBattleModeCatalog battleModeCatalog,
        CancellationToken cancellationToken) =>
    {
        if (!TryGetAuthenticatedUserId(
                user,
                out Guid userId))
        {
            return Results.Json(
                ApiResponse<BattleQueueStatusResponse>.Fail(
                    "invalid_access_token",
                    "The access token did not contain a valid user ID."),
                statusCode:
                    StatusCodes.Status401Unauthorized);
        }

        if (request.DeckId == Guid.Empty ||
            string.IsNullOrWhiteSpace(request.ModeId) ||
            string.IsNullOrWhiteSpace(request.VehicleId))
        {
            return Results.Json(
                ApiResponse<BattleQueueStatusResponse>.Fail(
                    "invalid_queue_request",
                    "Deck, mode and vehicle are required."),
                statusCode:
                    StatusCodes.Status400BadRequest);
        }

        if (!battleModeCatalog.TryGetMode(
                request.ModeId,
                out BattleModeDefinition? mode))
        {
            return Results.Json(
                ApiResponse<BattleQueueStatusResponse>.Fail(
                    "battle_mode_not_found",
                    "The requested battle mode does not exist."),
                statusCode:
                    StatusCodes.Status404NotFound);
        }

        if (!mode.IsAvailableAt(DateTime.UtcNow))
        {
            return Results.Json(
                ApiResponse<BattleQueueStatusResponse>.Fail(
                    "battle_mode_unavailable",
                    "The requested battle mode is currently unavailable."),
                statusCode:
                    StatusCodes.Status409Conflict);
        }

        UserServiceHangarFetchResult hangarResult =
            await hangarClient.GetHangarAsync(
                userId,
                cancellationToken);

        if (hangarResult.Status ==
            UserServiceHangarFetchStatus.NotFound)
        {
            return Results.Json(
                ApiResponse<BattleQueueStatusResponse>.Fail(
                    hangarResult.ErrorCode,
                    hangarResult.ErrorMessage),
                statusCode:
                    StatusCodes.Status404NotFound);
        }

        if (hangarResult.Status !=
                UserServiceHangarFetchStatus.Success ||
            hangarResult.Hangar is null)
        {
            return Results.Json(
                ApiResponse<BattleQueueStatusResponse>.Fail(
                    hangarResult.ErrorCode,
                    hangarResult.ErrorMessage),
                statusCode:
                    StatusCodes.Status503ServiceUnavailable);
        }

        HangarDeckResponse? selectedDeck =
            hangarResult.Hangar.Decks
                .FirstOrDefault(
                    deck =>
                        deck.DeckId ==
                        request.DeckId);

        if (selectedDeck is null)
        {
            return Results.Json(
                ApiResponse<BattleQueueStatusResponse>.Fail(
                    "deck_not_found",
                    "The selected deck does not belong to this account."),
                statusCode:
                    StatusCodes.Status404NotFound);
        }

        bool vehicleIsInDeck =
            selectedDeck.Slots.Any(
                slot =>
                    slot.Vehicle is not null &&
                    string.Equals(
                        slot.Vehicle.VehicleId,
                        request.VehicleId.Trim(),
                        StringComparison.OrdinalIgnoreCase));

        if (!vehicleIsInDeck)
        {
            return Results.Json(
                ApiResponse<BattleQueueStatusResponse>.Fail(
                    "vehicle_not_in_selected_deck",
                    "The selected vehicle is not part of the selected deck."),
                statusCode:
                    StatusCodes.Status409Conflict);
        }

        if (!vehicleCatalog.TryGetVehicle(
                request.VehicleId,
                out VehicleCatalogEntry? vehicle))
        {
            return Results.Json(
                ApiResponse<BattleQueueStatusResponse>.Fail(
                    "vehicle_catalog_mismatch",
                    "The selected vehicle could not be resolved in game data."),
                statusCode:
                    StatusCodes.Status502BadGateway);
        }

        bool vehicleIsAllowed =
            vehicle.BattleRatingTenths >=
                mode.VehicleRules
                    .MinimumBattleRatingTenths &&
            vehicle.BattleRatingTenths <=
                mode.VehicleRules
                    .MaximumBattleRatingTenths &&
            mode.VehicleRules
                .AllowedVehicleClasses
                .Any(
                    vehicleClass =>
                        string.Equals(
                            vehicleClass,
                            vehicle.VehicleClass,
                            StringComparison.OrdinalIgnoreCase));

        if (!vehicleIsAllowed)
        {
            return Results.Json(
                ApiResponse<BattleQueueStatusResponse>.Fail(
                    "vehicle_not_allowed_by_mode",
                    "The selected vehicle does not satisfy this battle mode's rules."),
                statusCode:
                    StatusCodes.Status409Conflict);
        }

        MatchmakingServiceCallResult<
            InternalBattleQueueStatusResponse> result =
            await matchmakingClient.JoinQueueAsync(
                new InternalJoinBattleQueueRequest(
                    userId,
                    selectedDeck.DeckId,
                    mode.ModeId,
                    vehicle.VehicleId,
                    vehicle.BattleRatingTenths,
                    vehicle.VehicleClass),
                cancellationToken);

        return result.Status switch
        {
            MatchmakingServiceCallStatus.Success
                when result.Data is not null =>
                Results.Ok(
                    ApiResponse<BattleQueueStatusResponse>
                        .Ok(
                            ToBattleQueueStatusResponse(
                                result.Data))),

            MatchmakingServiceCallStatus.BadRequest =>
                Results.Json(
                    ApiResponse<BattleQueueStatusResponse>.Fail(
                        result.ErrorCode,
                        result.ErrorMessage),
                    statusCode:
                        StatusCodes.Status400BadRequest),

            MatchmakingServiceCallStatus.Conflict =>
                Results.Json(
                    ApiResponse<BattleQueueStatusResponse>.Fail(
                        result.ErrorCode,
                        result.ErrorMessage),
                    statusCode:
                        StatusCodes.Status409Conflict),

            _ =>
                Results.Json(
                    ApiResponse<BattleQueueStatusResponse>.Fail(
                        result.ErrorCode,
                        result.ErrorMessage),
                    statusCode:
                        StatusCodes.Status503ServiceUnavailable)
        };
    })
    .RequireAuthorization();

app.MapGet(
    "/api/matchmaking/queue/status",
    async (
        ClaimsPrincipal user,
        MatchmakingServiceClient matchmakingClient,
        CancellationToken cancellationToken) =>
    {
        if (!TryGetAuthenticatedUserId(
                user,
                out Guid userId))
        {
            return Results.Json(
                ApiResponse<BattleQueueStatusResponse>.Fail(
                    "invalid_access_token",
                    "The access token did not contain a valid user ID."),
                statusCode:
                    StatusCodes.Status401Unauthorized);
        }

        MatchmakingServiceCallResult<
            InternalBattleQueueStatusResponse> result =
            await matchmakingClient.GetQueueStatusAsync(
                userId,
                cancellationToken);

        return result.Status switch
        {
            MatchmakingServiceCallStatus.Success
                when result.Data is not null =>
                Results.Ok(
                    ApiResponse<BattleQueueStatusResponse>
                        .Ok(
                            ToBattleQueueStatusResponse(
                                result.Data))),

            MatchmakingServiceCallStatus.BadRequest =>
                Results.Json(
                    ApiResponse<BattleQueueStatusResponse>.Fail(
                        result.ErrorCode,
                        result.ErrorMessage),
                    statusCode:
                        StatusCodes.Status400BadRequest),

            _ =>
                Results.Json(
                    ApiResponse<BattleQueueStatusResponse>.Fail(
                        result.ErrorCode,
                        result.ErrorMessage),
                    statusCode:
                        StatusCodes.Status503ServiceUnavailable)
        };
    })
    .RequireAuthorization();

app.MapPost(
    "/api/matchmaking/queue/leave",
    async (
        ClaimsPrincipal user,
        MatchmakingServiceClient matchmakingClient,
        CancellationToken cancellationToken) =>
    {
        if (!TryGetAuthenticatedUserId(
                user,
                out Guid userId))
        {
            return Results.Json(
                ApiResponse<BattleQueueLeaveResponse>.Fail(
                    "invalid_access_token",
                    "The access token did not contain a valid user ID."),
                statusCode:
                    StatusCodes.Status401Unauthorized);
        }

        MatchmakingServiceCallResult<
            InternalBattleQueueLeaveResponse> result =
            await matchmakingClient.LeaveQueueAsync(
                userId,
                cancellationToken);

        return result.Status switch
        {
            MatchmakingServiceCallStatus.Success
                when result.Data is not null =>
                Results.Ok(
                    ApiResponse<BattleQueueLeaveResponse>
                        .Ok(
                            ToBattleQueueLeaveResponse(
                                result.Data))),

            MatchmakingServiceCallStatus.BadRequest =>
                Results.Json(
                    ApiResponse<BattleQueueLeaveResponse>.Fail(
                        result.ErrorCode,
                        result.ErrorMessage),
                    statusCode:
                        StatusCodes.Status400BadRequest),

            _ =>
                Results.Json(
                    ApiResponse<BattleQueueLeaveResponse>.Fail(
                        result.ErrorCode,
                        result.ErrorMessage),
                    statusCode:
                        StatusCodes.Status503ServiceUnavailable)
        };
    })
    .RequireAuthorization();

app.MapPost(
    "/api/matchmaking/connection-ticket",
    async (
        ClaimsPrincipal user,
        HttpResponse response,
        MatchmakingServiceClient matchmakingClient,
        CancellationToken cancellationToken) =>
    {
        if (!TryGetAuthenticatedUserId(
                user,
                out Guid userId))
        {
            return Results.Json(
                ApiResponse<BattleConnectionTicketResponse>.Fail(
                    "invalid_access_token",
                    "The access token did not contain a valid user ID."),
                statusCode:
                    StatusCodes.Status401Unauthorized);
        }

        MatchmakingServiceCallResult<
            InternalBattleConnectionTicketResponse> result =
            await matchmakingClient
                .IssueConnectionTicketAsync(
                    userId,
                    cancellationToken);

        if (result.Status ==
                MatchmakingServiceCallStatus.Success &&
            result.Data is not null)
        {
            /*
             * Join-Tickets enthalten ein kurzlebiges Secret.
             * Browser/Proxies dürfen diese Response niemals cachen.
             */
            response.Headers.CacheControl =
                "no-store, no-cache, max-age=0";

            response.Headers.Pragma =
                "no-cache";

            return Results.Ok(
                ApiResponse<BattleConnectionTicketResponse>
                    .Ok(
                        ToBattleConnectionTicketResponse(
                            result.Data)));
        }

        if (result.Status ==
            MatchmakingServiceCallStatus.Conflict)
        {
            return Results.Json(
                ApiResponse<BattleConnectionTicketResponse>.Fail(
                    result.ErrorCode,
                    result.ErrorMessage),
                statusCode:
                    StatusCodes.Status409Conflict);
        }

        if (result.Status ==
            MatchmakingServiceCallStatus.BadRequest)
        {
            return Results.Json(
                ApiResponse<BattleConnectionTicketResponse>.Fail(
                    result.ErrorCode,
                    result.ErrorMessage),
                statusCode:
                    StatusCodes.Status400BadRequest);
        }

        return Results.Json(
            ApiResponse<BattleConnectionTicketResponse>.Fail(
                result.ErrorCode,
                result.ErrorMessage),
            statusCode:
                StatusCodes.Status503ServiceUnavailable);
    })
    .RequireAuthorization();

app.Run();



static TechTreeNodeProgressResponse
    ToTechTreeNodeProgressResponse(
        UserTechTreeNodeProgressResponse node)
{
    return new TechTreeNodeProgressResponse(
        node.NodeId,
        node.ResearchState,
        node.ResearchPointsApplied,
        node.ResearchStartedAtUtc,
        node.ResearchedAtUtc,
        node.PurchasedAtUtc,
        node.IsVehicleOwned);
}

static bool TryGetAuthenticatedUserId(
    ClaimsPrincipal user,
    out Guid userId)
{
    userId = Guid.Empty;

    return Guid.TryParse(
        user.FindFirstValue("user_id"),
        out userId) &&
        userId != Guid.Empty;
}

static BattleModeResponse ToBattleModeResponse(
    BattleModeDefinition mode)
{
    ArgumentNullException.ThrowIfNull(mode);

    return new BattleModeResponse(
        mode.ModeId,
        mode.Revision,
        mode.DisplayName,
        mode.Description,
        mode.Kind.ToString(),
        mode.VehicleRules.MinimumBattleRatingTenths,
        mode.VehicleRules.MaximumBattleRatingTenths,
        mode.Teams
            .Select(
                team =>
                    new BattleModeTeamResponse(
                        team.TeamId,
                        team.DisplayName,
                        team.RequiredPlayerCount))
            .ToArray());
}

static BattleQueueStatusResponse
    ToBattleQueueStatusResponse(
        InternalBattleQueueStatusResponse status)
{
    ArgumentNullException.ThrowIfNull(status);

    return new BattleQueueStatusResponse(
        status.HasActiveQueueEntry,
        status.QueueEntry is null
            ? null
            : ToBattleQueueEntryStatusResponse(
                status.QueueEntry),
        status.MatchStatus,
        status.MapId,
        status.IsReadyToConnect);
}

static BattleQueueEntryStatusResponse
    ToBattleQueueEntryStatusResponse(
        BattleQueueEntryResponse queueEntry)
{
    ArgumentNullException.ThrowIfNull(queueEntry);

    return new BattleQueueEntryStatusResponse(
        queueEntry.QueueEntryId,
        queueEntry.DeckId,
        queueEntry.ModeId,
        queueEntry.ModeRevision,
        queueEntry.InitialVehicleId,
        queueEntry.Status,
        queueEntry.MatchId,
        queueEntry.QueuedAtUtc,
        queueEntry.UpdatedAtUtc,
        queueEntry.MatchedAtUtc,
        queueEntry.FailureReason);
}

static BattleQueueLeaveResponse
    ToBattleQueueLeaveResponse(
        InternalBattleQueueLeaveResponse leaveResult)
{
    ArgumentNullException.ThrowIfNull(leaveResult);

    return new BattleQueueLeaveResponse(
        leaveResult.HadActiveQueueEntry,
        leaveResult.WasCancelled,
        leaveResult.MatchAlreadyFormed,
        ToBattleQueueStatusResponse(
            leaveResult.Status));
}

static BattleConnectionTicketResponse
    ToBattleConnectionTicketResponse(
        InternalBattleConnectionTicketResponse ticket)
{
    ArgumentNullException.ThrowIfNull(ticket);

    return new BattleConnectionTicketResponse(
        ticket.MatchId,
        ticket.TicketId,
        ticket.BattleServerPublicHost,
        ticket.BattleServerPublicPort,
        ticket.ExpiresAtUtc,
        ticket.SecretBase64);
}