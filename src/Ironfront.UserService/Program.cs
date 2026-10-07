using Ironfront.GameData.Application;
using Ironfront.GameData.Infrastructure.Json;
using Ironfront.Shared.Contracts.UserService;
using Ironfront.UserService.Application;
using Ironfront.UserService.Application.DependencyInjection;
using Ironfront.UserService.Application.Models;
using Ironfront.UserService.Infrastructure.DependencyInjection;
using Ironfront.UserService.Infrastructure.Persistence;
using Ironfront.UserService.Security;
using Microsoft.EntityFrameworkCore;
using Ironfront.UserService.Application.Exceptions;
using Ironfront.Shared.Contracts.Hangar;
using Ironfront.Shared.Contracts.TechTree;


string userDbConnection = GetRequiredEnvironmentVariable(
    "IRONFRONT_USER_DB_CONNECTION");

string defaultStarterVehicleId = GetRequiredEnvironmentVariable(
    "IRONFRONT_DEFAULT_STARTER_VEHICLE_ID");

string defaultStarterDeckName =
    Environment.GetEnvironmentVariable(
        "IRONFRONT_DEFAULT_STARTER_DECK_NAME")
    ?? "Starter Deck";

if (string.IsNullOrWhiteSpace(defaultStarterDeckName))
{
    throw new InvalidOperationException(
        "IRONFRONT_DEFAULT_STARTER_DECK_NAME must not be empty.");
}

string internalServiceKey = GetRequiredEnvironmentVariable(
    "IRONFRONT_INTERNAL_SERVICE_KEY");

int userServicePort = GetRequiredPort(
    "IRONFRONT_USERSERVICE_PORT");

string gameDataDirectory =
Environment.GetEnvironmentVariable("IRONFRONT_GAME_DATA_DIRECTORY")
?? Path.Combine(AppContext.BaseDirectory, "game-data");

string vehicleCatalogPath = Path.Combine(
    gameDataDirectory,
    "vehicles.catalog.json");

string techTreeCatalogPath = Path.Combine(
    gameDataDirectory,
    "tech-trees.catalog.json");

IVehicleCatalogLoader vehicleCatalogLoader =
    new JsonVehicleCatalogLoader();

ITechTreeCatalogLoader techTreeCatalogLoader =
    new JsonTechTreeCatalogLoader();

IVehicleCatalog vehicleCatalog;
ITechTreeCatalog techTreeCatalog;

try
{
    vehicleCatalog = vehicleCatalogLoader.LoadFromFile(
        vehicleCatalogPath);

    techTreeCatalog = techTreeCatalogLoader.LoadFromFile(
        techTreeCatalogPath,
        vehicleCatalog);
}
catch (Exception exception)
{
    throw new InvalidOperationException(
        $"Could not start UserService because game data could not be loaded from '{gameDataDirectory}'.",
        exception);
}

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(userServicePort);
});

builder.Services.AddSingleton<IVehicleCatalog>(vehicleCatalog);
builder.Services.AddSingleton<ITechTreeCatalog>(techTreeCatalog);
builder.Services.AddSingleton(new StarterVehicleGrant(
    defaultStarterVehicleId));

builder.Services.AddSingleton(new StarterDeckGrant(
    defaultStarterDeckName));

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddUserApplication();
builder.Services.AddUserInfrastructure(userDbConnection);

var app = builder.Build();

app.Logger.LogInformation(
    "UserService started on localhost:{Port}. " +
    "Vehicle catalog: {VehicleVersion} ({VehicleCount} vehicles). " +
    "Tech tree catalog: {TechTreeVersion} ({TechTreeCount} trees, {NodeCount} nodes).",
    userServicePort,
    vehicleCatalog.Manifest.Version,
    vehicleCatalog.Manifest.VehicleCount,
    techTreeCatalog.Manifest.Version,
    techTreeCatalog.Manifest.TechTreeCount,
    techTreeCatalog.Manifest.NodeCount);

app.MapGet("/internal/health", () =>
{
    return Results.Ok(new
    {
        status = "ok",
        service = "user-service"
    });
});

app.MapGet(
    "/internal/db-health",
    async (
        UserDbContext dbContext,
        CancellationToken cancellationToken) =>
        {
            bool canConnect = await dbContext.Database.CanConnectAsync(
                cancellationToken);

            return canConnect
            ? Results.Ok(new
            {
                status = "ok",
                database = "reachable"
            })
            : Results.Problem(
                title: "UserService database unavailable.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        });

app.MapPost(
    "/internal/users/provision",
    async (
        HttpRequest httpRequest,
        ProvisionUserRequest request,
        IUserProvisioningService provisioningService,
        CancellationToken cancellationToken) =>
        {
            if (!InternalServiceKeyValidator.IsValid(
                httpRequest,
                internalServiceKey))
            {
                return Results.Unauthorized();
            }

            try
            {
                ProvisionedUserProfile profile =
                await provisioningService.ProvisionAsync(
                    new ProvisionUserCommand(
                        request.UserId,
                        request.Username,
                        request.Email),
                        cancellationToken);

                var response = new ProvisionUserResponse(
                    profile.UserId,
                    profile.Username,
                    profile.Email,
                    profile.ActiveDeckId,
                    profile.CreatedAtUtc,
                    profile.UpdatedAtUtc,
                    profile.WasCreated);

                return Results.Ok(response);
            }
            catch (ArgumentException exception)
            {
                return Results.BadRequest(new
                {
                    error = exception.Message
                });
            }
        });

app.MapGet(
    "/internal/users/{userId:guid}/hangar",
    async (
        HttpRequest httpRequest,
        Guid userId,
        IUserHangarService hangarService,
        CancellationToken cancellationToken) =>
    {
        if (!InternalServiceKeyValidator.IsValid(
                httpRequest,
                internalServiceKey))
        {
            return Results.Unauthorized();
        }

        try
        {
            UserHangarSnapshot hangar =
                await hangarService.GetHangarAsync(
                    userId,
                    cancellationToken);

            var response = new UserHangarSnapshotResponse(
                hangar.UserId,
                hangar.ActiveDeckId,
                hangar.OwnedVehicles
                    .Select(ToHangarVehicleResponse)
                    .ToArray(),
                hangar.Decks
                    .Select(ToHangarDeckResponse)
                    .ToArray());

            return Results.Ok(response);
        }
        catch (UserHangarNotFoundException)
        {
            return Results.NotFound(new
            {
                error = "user_hangar_not_found",
                message = "No user profile exists for this account."
            });
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new
            {
                error = "invalid_request",
                message = exception.Message
            });
        }
        catch (InvalidOperationException exception)
        {
            app.Logger.LogError(
                exception,
                "Could not build hangar data for user {UserId}.",
                userId);

            return Results.Problem(
                title: "User hangar could not be loaded.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    });

app.MapPost(
    "/internal/users/{userId:guid}/decks",
    async (
        HttpRequest httpRequest,
        Guid userId,
        CreateUserDeckRequest request,
        IUserDeckService userDeckService,
        CancellationToken cancellationToken) =>
    {
        if (!InternalServiceKeyValidator.IsValid(
                httpRequest,
                internalServiceKey))
        {
            return Results.Unauthorized();
        }

        try
        {
            UserDeckCreationResult createdDeck =
                await userDeckService.CreateDeckAsync(
                    new CreateUserDeckCommand(
                        userId,
                        request.Name,
                        request.Nation),
                    cancellationToken);

            var response = new CreateUserDeckResponse(
                createdDeck.UserId,
                createdDeck.DeckId,
                createdDeck.Name,
                createdDeck.Nation,
                createdDeck.IsActive,
                createdDeck.CreatedAtUtc,
                createdDeck.UpdatedAtUtc);

            return Results.Created(
                $"/internal/users/{userId}/decks/{createdDeck.DeckId}",
                response);
        }
        catch (UserProfileNotFoundException)
        {
            return Results.NotFound(new
            {
                error = "user_profile_not_found",
                message = "No user profile exists for this account."
            });
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new
            {
                error = "invalid_deck_request",
                message = exception.Message
            });
        }
    });

app.MapPut(
    "/internal/users/{userId:guid}/decks/{deckId:guid}/active",
    async (
        HttpRequest httpRequest,
        Guid userId,
        Guid deckId,
        IUserDeckService userDeckService,
        CancellationToken cancellationToken) =>
    {
        if (!InternalServiceKeyValidator.IsValid(
                httpRequest,
                internalServiceKey))
        {
            return Results.Unauthorized();
        }

        try
        {
            UserDeckActivationResult activatedDeck =
                await userDeckService.ActivateDeckAsync(
                    new ActivateUserDeckCommand(
                        userId,
                        deckId),
                    cancellationToken);

            var response = new UserDeckActivationResponse(
                activatedDeck.UserId,
                activatedDeck.DeckId,
                activatedDeck.Name,
                activatedDeck.Nation,
                activatedDeck.IsActive,
                activatedDeck.CreatedAtUtc,
                activatedDeck.UpdatedAtUtc);

            return Results.Ok(response);
        }
        catch (UserProfileNotFoundException)
        {
            return Results.NotFound(new
            {
                error = "user_profile_not_found",
                message = "No user profile exists for this account."
            });
        }
        catch (UserDeckNotFoundException)
        {
            return Results.NotFound(new
            {
                error = "user_deck_not_found",
                message = "The requested deck does not exist for this account."
            });
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new
            {
                error = "invalid_deck_activation_request",
                message = exception.Message
            });
        }
    });

app.MapPut(
    "/internal/users/{userId:guid}/decks/{deckId:guid}/slots/{slotIndex:int}",
    async (
        HttpRequest httpRequest,
        Guid userId,
        Guid deckId,
        int slotIndex,
        AssignUserDeckSlotVehicleRequest request,
        IUserDeckService userDeckService,
        CancellationToken cancellationToken) =>
    {
        if (!InternalServiceKeyValidator.IsValid(
                httpRequest,
                internalServiceKey))
        {
            return Results.Unauthorized();
        }

        try
        {
            UserDeckSlotAssignmentResult assignment =
                await userDeckService.AssignVehicleToDeckSlotAsync(
                    new AssignUserDeckVehicleCommand(
                        userId,
                        deckId,
                        slotIndex,
                        request.VehicleId),
                    cancellationToken);

            var response = new UserDeckSlotAssignmentResponse(
                assignment.UserId,
                assignment.DeckId,
                assignment.SlotIndex,
                assignment.VehicleId,
                assignment.AddedAtUtc);

            return Results.Ok(response);
        }
        catch (UserProfileNotFoundException)
        {
            return Results.NotFound(new
            {
                error = "user_profile_not_found",
                message = "No user profile exists for this account."
            });
        }
        catch (UserDeckNotFoundException)
        {
            return Results.NotFound(new
            {
                error = "user_deck_not_found",
                message = "The requested deck does not exist for this account."
            });
        }
        catch (UserVehicleNotOwnedException)
        {
            return Results.Json(
                new
                {
                    error = "user_vehicle_not_owned",
                    message = "The requested vehicle is not owned by this account."
                },
                statusCode: StatusCodes.Status403Forbidden);
        }
        catch (UserDeckVehicleNationMismatchException)
        {
            return Results.Json(
                new
                {
                    error = "user_deck_vehicle_nation_mismatch",
                    message = "The requested vehicle does not match the deck nation."
                },
                statusCode: StatusCodes.Status409Conflict);
        }
        catch (UserDeckVehicleAlreadyAssignedException)
        {
            return Results.Json(
                new
                {
                    error = "user_deck_vehicle_already_assigned",
                    message = "The requested vehicle is already assigned to another slot in this deck."
                },
                statusCode: StatusCodes.Status409Conflict);
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new
            {
                error = "invalid_deck_slot_assignment_request",
                message = exception.Message
            });
        }
    });

app.MapDelete(
    "/internal/users/{userId:guid}/decks/{deckId:guid}/slots/{slotIndex:int}",
    async (
        HttpRequest httpRequest,
        Guid userId,
        Guid deckId,
        int slotIndex,
        IUserDeckService userDeckService,
        CancellationToken cancellationToken) =>
    {
        if (!InternalServiceKeyValidator.IsValid(
                httpRequest,
                internalServiceKey))
        {
            return Results.Unauthorized();
        }

        try
        {
            UserDeckSlotClearResult clearResult =
                await userDeckService.ClearDeckSlotAsync(
                    new ClearUserDeckSlotCommand(
                        userId,
                        deckId,
                        slotIndex),
                    cancellationToken);

            UserDeckSlotClearResponse response = new(
                clearResult.UserId,
                clearResult.DeckId,
                clearResult.SlotIndex,
                clearResult.WasCleared,
                clearResult.ClearedAtUtc);

            return Results.Ok(response);
        }
        catch (UserProfileNotFoundException)
        {
            return Results.NotFound(new
            {
                error = "user_profile_not_found",
                message = "No user profile exists for this account."
            });
        }
        catch (UserDeckNotFoundException)
        {
            return Results.NotFound(new
            {
                error = "user_deck_not_found",
                message = "The requested deck does not exist for this account."
            });
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new
            {
                error = "invalid_deck_slot_clear_request",
                message = exception.Message
            });
        }
    });

app.MapGet(
    "/internal/users/{userId:guid}/tech-trees/{techTreeId}/progress",
    async (
        HttpRequest httpRequest,
        Guid userId,
        string techTreeId,
        IUserTechTreeProgressService techTreeProgressService,
        CancellationToken cancellationToken) =>
    {
        if (!InternalServiceKeyValidator.IsValid(
                httpRequest,
                internalServiceKey))
        {
            return Results.Unauthorized();
        }

        try
        {
            UserTechTreeProgressSnapshot snapshot =
                await techTreeProgressService.GetProgressAsync(
                    userId,
                    techTreeId,
                    cancellationToken);

            var response = new UserTechTreeProgressResponse(
                snapshot.UserId,
                snapshot.TechTreeId,
                snapshot.SelectedNodeId,
                snapshot.Nodes
                    .Select(ToUserTechTreeNodeProgressResponse)
                    .ToArray());

            return Results.Ok(response);
        }
        catch (UserProfileNotFoundException)
        {
            return Results.NotFound(new
            {
                error = "user_profile_not_found",
                message = "No user profile exists for this account."
            });
        }
        catch (TechTreeNotFoundException)
        {
            return Results.NotFound(new
            {
                error = "tech_tree_not_found",
                message = "The requested tech tree does not exist in the active game-data catalog."
            });
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new
            {
                error = "invalid_tech_tree_progress_request",
                message = exception.Message
            });
        }
        catch (InvalidOperationException exception)
        {
            app.Logger.LogError(
                exception,
                "Could not build tech tree progress for user {UserId} and tree {TechTreeId}.",
                userId,
                techTreeId);

            return Results.Problem(
                title: "Tech tree progress could not be loaded.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    });

app.MapPut(
    "/internal/users/{userId:guid}/tech-trees/{techTreeId}/research-target",
    async (
        HttpRequest httpRequest,
        Guid userId,
        string techTreeId,
        SelectResearchTargetRequest request,
        IUserTechTreeResearchService
            techTreeResearchService,
        CancellationToken cancellationToken) =>
    {
        if (!InternalServiceKeyValidator.IsValid(
                httpRequest,
                internalServiceKey))
        {
            return Results.Unauthorized();
        }

        if (request is null ||
            string.IsNullOrWhiteSpace(request.NodeId))
        {
            return Results.BadRequest(new
            {
                error = "invalid_research_target",
                message = "A research node ID is required."
            });
        }

        try
        {
            UserTechTreeProgressSnapshot snapshot =
                await techTreeResearchService
                    .SelectResearchTargetAsync(
                        userId,
                        techTreeId,
                        request.NodeId,
                        cancellationToken);

            var response = new UserTechTreeProgressResponse(
                snapshot.UserId,
                snapshot.TechTreeId,
                snapshot.SelectedNodeId,
                snapshot.Nodes
                    .Select(
                        ToUserTechTreeNodeProgressResponse)
                    .ToArray());

            return Results.Ok(response);
        }
        catch (UserProfileNotFoundException)
        {
            return Results.NotFound(new
            {
                error = "user_profile_not_found",
                message = "No user profile exists for this account."
            });
        }
        catch (TechTreeNotFoundException)
        {
            return Results.NotFound(new
            {
                error = "tech_tree_not_found",
                message = "The requested tech tree does not exist."
            });
        }
        catch (ResearchTargetNotAvailableException exception)
        {
            return Results.Conflict(new
            {
                error = "research_target_unavailable",
                message = exception.Message
            });
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new
            {
                error = "invalid_research_target",
                message = exception.Message
            });
        }
        catch (InvalidOperationException exception)
        {
            app.Logger.LogError(
                exception,
                "Could not select research target {NodeId} " +
                "for user {UserId} in tech tree {TechTreeId}.",
                request.NodeId,
                userId,
                techTreeId);

            return Results.Problem(
                title: "Research target could not be selected.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    });

app.Run();

static string GetRequiredEnvironmentVariable(string variableName)
{
    string? value = Environment.GetEnvironmentVariable(variableName);

    if (string.IsNullOrWhiteSpace(value))
    {
        throw new InvalidOperationException(
            $"Missing environment variable '{variableName}'.");
    }

    return value.Trim();
}

static int GetRequiredPort(string variableName)
{
    string value = GetRequiredEnvironmentVariable(variableName);

    if (!int.TryParse(value, out int port)
        || port is < 1 or > 65535)
    {
        throw new InvalidOperationException(
            $"Environment variable '{variableName}' must contain a valid TCP port.");
    }

    return port;
}

static HangarVehicleResponse ToHangarVehicleResponse(
    HangarVehicleInfo vehicle)
{
    return new HangarVehicleResponse(
        vehicle.VehicleId,
        vehicle.DisplayName,
        vehicle.Nation,
        vehicle.VehicleClass,
        vehicle.BattleRatingTenths);
}

static HangarDeckResponse ToHangarDeckResponse(
    HangarDeck deck)
{
    return new HangarDeckResponse(
        deck.DeckId,
        deck.Name,
        deck.Nation,
        deck.IsActive,
        deck.BattleRatingTenths,
        deck.Slots
            .Select(slot => new HangarDeckSlotResponse(
                slot.SlotIndex,
                slot.Vehicle is null
                    ? null
                    : ToHangarVehicleResponse(slot.Vehicle)))
            .ToArray());
}


static UserTechTreeNodeProgressResponse
    ToUserTechTreeNodeProgressResponse(
        UserTechTreeNodeProgress node)
{
    return new UserTechTreeNodeProgressResponse(
        node.NodeId,
        ToResearchStateName(node.ResearchState),
        node.ResearchPointsApplied,
        node.ResearchStartedAtUtc,
        node.ResearchedAtUtc,
        node.PurchasedAtUtc,
        node.IsVehicleOwned);
}

static string ToResearchStateName(
    TechTreeNodeResearchState researchState)
{
    return researchState switch
    {
        TechTreeNodeResearchState.Locked => "locked",
        TechTreeNodeResearchState.Available => "available",
        TechTreeNodeResearchState.Researching => "researching",
        TechTreeNodeResearchState.Researched => "researched",
        _ => throw new ArgumentOutOfRangeException(
            nameof(researchState),
            researchState,
            "Unknown tech tree research state.")
    };
}