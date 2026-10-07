using Ironfront.Matchmaking.Application;
using Ironfront.Matchmaking.Application.DependencyInjection;
using Ironfront.Matchmaking.Application.Exceptions;
using Ironfront.Matchmaking.Application.Models;
using Ironfront.Matchmaking.Domain.Enums;
using Ironfront.Matchmaking.Infrastructure.DependencyInjection;
using Ironfront.Matchmaking.Infrastructure.Persistence;
using Ironfront.MatchmakingService.Security;
using Ironfront.Shared.Contracts.Matchmaking;
using Ironfront.Matchmaking.Application.Configuration;
using Ironfront.MatchmakingService.Hosting;
using Ironfront.GameData.Application;
using Ironfront.GameData.Infrastructure.Json;
using Ironfront.GameData.Domain;

string matchmakingDbConnection =
    GetRequiredEnvironmentVariable(
        "IRONFRONT_MATCHMAKING_DB_CONNECTION");

string battleServerControlKey =
    GetRequiredEnvironmentVariable(
        "IRONFRONT_BATTLE_SERVER_CONTROL_KEY");

string internalServiceKey =
    GetRequiredEnvironmentVariable(
        "IRONFRONT_INTERNAL_SERVICE_KEY");

if (internalServiceKey.Length < 32)
{
    throw new InvalidOperationException(
        "IRONFRONT_INTERNAL_SERVICE_KEY must be at least 32 characters long.");
}

if (battleServerControlKey.Length < 32)
{
    throw new InvalidOperationException(
        "IRONFRONT_BATTLE_SERVER_CONTROL_KEY must be at least 32 characters long.");
}

int matchmakingPort = GetRequiredPort(
    "IRONFRONT_MATCHMAKING_PORT");

int heartbeatTimeoutSeconds =
    GetPositiveIntEnvironmentVariable(
        "IRONFRONT_BATTLE_SERVER_HEARTBEAT_TIMEOUT_SECONDS",
        defaultValue: 15);

int reconciliationIntervalSeconds =
    GetPositiveIntEnvironmentVariable(
        "IRONFRONT_BATTLE_SERVER_RECONCILIATION_INTERVAL_SECONDS",
        5);

int matchmakingIntervalMilliseconds =
    GetPositiveIntEnvironmentVariable(
        "IRONFRONT_MATCHMAKING_INTERVAL_MILLISECONDS",
        1000);

int matchmakingMaximumMatchesPerModePerCycle =
    GetPositiveIntEnvironmentVariable(
        "IRONFRONT_MATCHMAKING_MAXIMUM_MATCHES_PER_MODE_PER_CYCLE",
        8);

int battleJoinTicketLifetimeSeconds =
    GetPositiveIntEnvironmentVariable(
        "IRONFRONT_BATTLE_JOIN_TICKET_LIFETIME_SECONDS",
        
        90);
bool developmentEndpointsEnabled =
    GetEnvironmentVariableAsBoolean(
        "IRONFRONT_MATCHMAKING_ENABLE_DEVELOPMENT_ENDPOINTS",
        false);

TimeSpan readyHeartbeatTimeout =
    TimeSpan.FromSeconds(heartbeatTimeoutSeconds);

var builder = WebApplication.CreateBuilder(args);

#region GAME DATA
string gameDataDirectory =
    Environment.GetEnvironmentVariable(
        "IRONFRONT_GAME_DATA_DIRECTORY")
    ?? Path.Combine(
        AppContext.BaseDirectory,
        "game-data");

string battleModeCatalogPath =
    Path.Combine(
        gameDataDirectory,
        "battle-modes.catalog.json");

IBattleModeCatalogLoader
    battleModeCatalogLoader =
        new JsonBattleModeCatalogLoader();

IBattleModeCatalog battleModeCatalog;

try
{
    battleModeCatalog =
        battleModeCatalogLoader.LoadFromFile(
            battleModeCatalogPath);
}
catch (Exception exception)
{
    throw new InvalidOperationException(
        "Could not start MatchmakingService because " +
        $"battle mode data could not be loaded from " +
        $"'{gameDataDirectory}'.",
        exception);
}

builder.Services.AddSingleton<
    IBattleModeCatalog>(
    battleModeCatalog);
#endregion

builder.WebHost.ConfigureKestrel(options =>
{
    /*
     * The service remains private on localhost.
     *
     * Development:
     * Unity BattleServer on the same machine can reach it
     * through 127.0.0.1.
     *
     * Production:
     * Caddy exposes a TLS-protected control subdomain and
     * reverse-proxies requests to this local service.
     */
    options.ListenLocalhost(matchmakingPort);
});

builder.Services.AddSingleton(
    new BattleJoinTicketPolicy(
        TimeSpan.FromSeconds(
            battleJoinTicketLifetimeSeconds)));

builder.Services.AddSingleton(
    new BattleServerSelectionPolicy(
        TimeSpan.FromSeconds(
            heartbeatTimeoutSeconds)));

builder.Services.AddSingleton(
    new BattleServerHealthReconciliationOptions(
        TimeSpan.FromSeconds(
            reconciliationIntervalSeconds)));

builder.Services.AddSingleton(
    new BattleMatchmakingOptions(
        TimeSpan.FromMilliseconds(
            matchmakingIntervalMilliseconds),
        matchmakingMaximumMatchesPerModePerCycle));

builder.Services.AddHostedService<
    BattleMatchmakerWorker>();

builder.Services.AddHostedService<
    BattleServerHealthReconciliationWorker>();

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddMatchmakingApplication();
builder.Services.AddMatchmakingInfrastructure(
    matchmakingDbConnection);

var app = builder.Build();

app.Logger.LogInformation(
    "MatchmakingService started on localhost:{Port}. " +
    "Battle server heartbeat timeout: {TimeoutSeconds} seconds.",
    matchmakingPort,
    heartbeatTimeoutSeconds);

app.Logger.LogInformation(
    "Battle mode catalog {Version} loaded with {ModeCount} mode definitions.",
    battleModeCatalog.Manifest.Version,
    battleModeCatalog.Manifest.ModeCount);

app.MapGet("/control/v1/health", () =>
{
    return Results.Ok(new
    {
        status = "ok",
        service = "matchmaking-service"
    });
});

app.MapGet(
    "/control/v1/db-health",
    async (
        MatchmakingDbContext dbContext,
        CancellationToken cancellationToken) =>
    {
        bool canConnect =
            await dbContext.Database.CanConnectAsync(
                cancellationToken);

        return canConnect
            ? Results.Ok(new
            {
                status = "ok",
                database = "reachable"
            })
            : Results.Problem(
                title :
                    "Matchmaking database unavailable.",
                statusCode :
                    StatusCodes.Status503ServiceUnavailable);
    });

app.MapPost(
    "/control/v1/battle-servers/register",
    async (
        HttpRequest httpRequest,
        BattleServerRegistrationRequest registration,
        IBattleServerRegistry registry,
        CancellationToken cancellationToken) =>
    {
        if (!BattleServerControlKeyValidator.IsValid(
                httpRequest,
                battleServerControlKey))
        {
            return Results.Unauthorized();
        }

        try
        {
            BattleServerSlotSnapshot slot =
                await registry.RegisterAsync(
                    new BattleServerRegistrationCommand(
                        registration.ServerInstanceId,
                        registration.HostId,
                        registration.Region,
                        registration.PublicHost,
                        registration.PublicPort,
                        registration.BuildVersion,
                        registration.CatalogVersion,
                        registration.MaxPlayers),
                    cancellationToken);

            app.Logger.LogInformation(
                "Battle server slot {ServerInstanceId} " +
                "registered from host {HostId}. " +
                "Endpoint: {PublicHost}:{PublicPort}.",
                slot.ServerInstanceId,
                slot.HostId,
                slot.PublicHost,
                slot.PublicPort);

            return Results.Ok(ToResponse(slot));
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new
            {
                error = "invalid_battle_server_registration",
                message = exception.Message
            });
        }
    });

app.MapPost(
    "/control/v1/battle-servers/{serverInstanceId}/heartbeat",
    async (
        HttpRequest httpRequest,
        string serverInstanceId,
        BattleServerHeartbeatRequest heartbeat,
        IBattleServerRegistry registry,
        CancellationToken cancellationToken) =>
    {
        if (!BattleServerControlKeyValidator.IsValid(
                httpRequest,
                battleServerControlKey))
        {
            return Results.Unauthorized();
        }

        try
        {
            BattleServerSlotSnapshot slot =
                await registry.RecordHeartbeatAsync(
                    new BattleServerHeartbeatCommand(
                        serverInstanceId,
                        heartbeat.PlayerCount),
                    cancellationToken);

            return Results.Ok(ToResponse(slot));
        }
        catch (BattleServerSlotNotFoundException)
        {
            return Results.NotFound(new
            {
                error = "battle_server_slot_not_found",
                message =
                    "The battle server slot must register before sending heartbeats."
            });
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new
            {
                error = "invalid_battle_server_heartbeat",
                message = exception.Message
            });
        }
    });

app.MapPost(
    "/control/v1/battle-servers/{serverInstanceId}/commands/poll",
    async (
        HttpRequest httpRequest,
        string serverInstanceId,
        IBattleServerCommandInbox commandInbox,
        CancellationToken cancellationToken) =>
    {
        if (!BattleServerControlKeyValidator.IsValid(
                httpRequest,
                battleServerControlKey))
        {
            return Results.Unauthorized();
        }

        try
        {
            BattleServerCommandSnapshot? command =
                await commandInbox.PollNextAsync(
                    serverInstanceId,
                    cancellationToken);

            if (command is null)
                return Results.NoContent();

            return Results.Ok(
                new BattleServerCommandPollResponse(
                    command.CommandId,
                    command.CommandType,
                    command.PayloadJson,
                    command.CreatedAtUtc,
                    command.DeliveredAtUtc));
        }
        catch (BattleServerSlotNotFoundException)
        {
            return Results.NotFound(new
            {
                error = "battle_server_slot_not_found",
                message =
                    "The battle server slot must register before polling commands."
            });
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new
            {
                error = "invalid_battle_server_command_poll",
                message = exception.Message
            });
        }
    });

app.MapPost(
    "/control/v1/battle-servers/{serverInstanceId}/commands/{commandId:guid}/ack",
    async (
        HttpRequest httpRequest,
        string serverInstanceId,
        Guid commandId,
        BattleServerCommandAcknowledgementRequest acknowledgement,
        IBattleServerCommandInbox commandInbox,
        CancellationToken cancellationToken) =>
    {
        if (!BattleServerControlKeyValidator.IsValid(
                httpRequest,
                battleServerControlKey))
        {
            return Results.Unauthorized();
        }

        try
        {
            BattleServerCommandSnapshot command =
                await commandInbox.AcknowledgeAsync(
                    serverInstanceId,
                    new BattleServerCommandAcknowledgement(
                        commandId,
                        acknowledgement.Succeeded,
                        acknowledgement.FailureReason),
                    cancellationToken);

            return Results.Ok(
                new BattleServerCommandAcknowledgementResponse(
                    command.CommandId,
                    ToApiCommandState(command.State),
                    command.AcknowledgedAtUtc,
                    command.FailedAtUtc,
                    command.FailureReason));
        }
        catch (BattleServerCommandNotFoundException)
        {
            return Results.NotFound(new
            {
                error = "battle_server_command_not_found",
                message =
                    "The requested battle server command does not exist."
            });
        }
        catch (BattleServerCommandOwnershipException)
        {
            /*
             * Deliberately return 404 rather than revealing command
             * ownership details to another battle server slot.
             */
            return Results.NotFound(new
            {
                error = "battle_server_command_not_found",
                message =
                    "The requested battle server command does not exist."
            });
        }
        catch (InvalidOperationException exception)
        {
            return Results.Conflict(new
            {
                error = "invalid_battle_server_command_state",
                message = exception.Message
            });
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new
            {
                error = "invalid_battle_server_command_acknowledgement",
                message = exception.Message
            });
        }
    });

app.MapGet(
    "/control/v1/battle-servers/ready",
    async (
        HttpRequest httpRequest,
        IBattleServerRegistry registry,
        CancellationToken cancellationToken) =>
    {
        if (!BattleServerControlKeyValidator.IsValid(
                httpRequest,
                battleServerControlKey))
        {
            return Results.Unauthorized();
        }

        IReadOnlyList<BattleServerSlotSnapshot> readySlots =
            await registry.GetReadySlotsAsync(
                readyHeartbeatTimeout,
                cancellationToken);

        return Results.Ok(
            readySlots
                .Select(ToResponse)
                .ToArray());
    });

app.MapPost(
    "/control/v1/battle-servers/{serverInstanceId}/matches/{matchId:guid}/cancel",
    async (
        HttpRequest httpRequest,
        string serverInstanceId,
        Guid matchId,
        BattleMatchCancellationRequest request,
        IBattleMatchLifecycle battleMatchLifecycle,
        CancellationToken cancellationToken) =>
    {
        if (!BattleServerControlKeyValidator.IsValid(
                httpRequest,
                battleServerControlKey))
        {
            return Results.Unauthorized();
        }

        try
        {
            BattleMatchSnapshot battleMatch =
                await battleMatchLifecycle.CancelAsync(
                    serverInstanceId,
                    matchId,
                    request.Reason,
                    cancellationToken);

            return Results.Ok(
                ToBattleMatchResponse(battleMatch));
        }
        catch (BattleMatchNotFoundException)
        {
            return Results.NotFound(new
            {
                error = "battle_match_not_found",
                message =
                    "The requested battle match does not exist."
            });
        }
        catch (BattleMatchOwnershipException)
        {
            /*
             * Do not reveal that a match exists on another slot.
             */
            return Results.NotFound(new
            {
                error = "battle_match_not_found",
                message =
                    "The requested battle match does not exist."
            });
        }
        catch (BattleServerSlotNotFoundException)
        {
            return Results.NotFound(new
            {
                error = "battle_server_slot_not_found",
                message =
                    "The battle server slot does not exist."
            });
        }
        catch (InvalidOperationException exception)
        {
            return Results.Conflict(new
            {
                error = "invalid_battle_match_state",
                message = exception.Message
            });
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new
            {
                error = "invalid_battle_match_cancel_request",
                message = exception.Message
            });
        }
    });

app.MapPost(
    "/control/v1/battle-servers/{serverInstanceId}/matches/{matchId:guid}/ready",
    async (
        HttpRequest httpRequest,
        string serverInstanceId,
        Guid matchId,
        IBattleMatchLifecycle battleMatchLifecycle,
        CancellationToken cancellationToken) =>
    {
        if (!BattleServerControlKeyValidator.IsValid(
                httpRequest,
                battleServerControlKey))
        {
            return Results.Unauthorized();
        }

        try
        {
            BattleMatchSnapshot battleMatch =
                await battleMatchLifecycle
                    .MarkReadyForPlayersAsync(
                        serverInstanceId,
                        matchId,
                        cancellationToken);

            return Results.Ok(
                ToBattleMatchResponse(battleMatch));
        }
        catch (BattleMatchNotFoundException)
        {
            return Results.NotFound(new
            {
                error = "battle_match_not_found",
                message =
                    "The requested battle match does not exist."
            });
        }
        catch (BattleMatchOwnershipException)
        {
            /*
             * Nicht verraten, welchem anderen Slot ein Match gehört.
             */
            return Results.NotFound(new
            {
                error = "battle_match_not_found",
                message =
                    "The requested battle match does not exist."
            });
        }
        catch (BattleServerSlotNotFoundException)
        {
            return Results.NotFound(new
            {
                error = "battle_server_slot_not_found",
                message =
                    "The battle server slot does not exist."
            });
        }
        catch (InvalidOperationException exception)
        {
            return Results.Conflict(new
            {
                error = "invalid_battle_match_state",
                message = exception.Message
            });
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new
            {
                error = "invalid_battle_match_ready_request",
                message = exception.Message
            });
        }
    });

app.MapPost(
    "/control/v1/battle-servers/{serverInstanceId}/join-tickets/validate",
    async (
        HttpRequest httpRequest,
        string serverInstanceId,
        BattleJoinTicketValidationRequest request,
        IBattleJoinTicketValidator battleJoinTicketValidator,
        CancellationToken cancellationToken) =>
    {
        if (!BattleServerControlKeyValidator.IsValid(
                httpRequest,
                battleServerControlKey))
        {
            return Results.Unauthorized();
        }

        BattleJoinTicketValidationSnapshot validation =
            await battleJoinTicketValidator
                .ValidateAndConsumeAsync(
                    serverInstanceId,
                    new ValidateBattleJoinTicketRequest(
                        request.MatchId,
                        request.TicketId,
                        request.SecretBase64),
                    cancellationToken);

        return Results.Ok(
            new BattleJoinTicketValidationResponse(
                validation.Approved,
                validation.RejectionCode,
                validation.MatchId,
                validation.MatchPlayerId,
                validation.UserId,
                validation.InitialVehicleId,
                validation.TeamId,
                validation.TeamSlotIndex,
                validation.PlayerSlotIndex));
    });

app.MapPost(
    "/control/v1/battle-servers/{serverInstanceId}/matches/{matchId:guid}/players/{matchPlayerId:guid}/disconnected",
    async (
        HttpRequest httpRequest,
        string serverInstanceId,
        Guid matchId,
        Guid matchPlayerId,
        IBattleMatchPlayerConnectionLifecycle
            battleMatchPlayerConnectionLifecycle,
        CancellationToken cancellationToken) =>
    {
        if (!BattleServerControlKeyValidator.IsValid(
                httpRequest,
                battleServerControlKey))
        {
            return Results.Unauthorized();
        }

        try
        {
            await battleMatchPlayerConnectionLifecycle
                .MarkDisconnectedAsync(
                    serverInstanceId,
                    matchId,
                    matchPlayerId,
                    cancellationToken);

            return Results.NoContent();
        }
        catch (BattleMatchNotFoundException)
        {
            return Results.NotFound(new
            {
                error = "battle_match_not_found",
                message =
                    "The requested battle match does not exist."
            });
        }
        catch (BattleMatchPlayerNotFoundException)
        {
            return Results.NotFound(new
            {
                error = "battle_match_player_not_found",
                message =
                    "The requested battle match player does not exist."
            });
        }
        catch (InvalidOperationException)
        {
            return Results.Conflict(new
            {
                error = "battle_match_player_disconnect_conflict",
                message =
                    "The requested player cannot be disconnected " +
                    "from the current battle state."
            });
        }
        catch (ArgumentException)
        {
            return Results.BadRequest(new
            {
                error = "invalid_battle_match_player_disconnect_request",
                message =
                    "The disconnect request contains invalid identifiers."
            });
        }
    });

app.MapPost(
    "/internal/v1/battle-queue/join",
    async (
        HttpRequest httpRequest,
        InternalJoinBattleQueueRequest request,
        IBattleQueueLifecycle battleQueueLifecycle,
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
            BattleQueueStatusSnapshot status =
                await battleQueueLifecycle.JoinAsync(
                    new JoinBattleQueueCommand(
                        request.UserId,
                        request.DeckId,
                        request.ModeId,
                        request.InitialVehicleId,
                        request.VehicleBattleRatingTenths,
                        request.VehicleClass),
                    cancellationToken);

            return Results.Ok(
                ToInternalBattleQueueStatusResponse(
                    status));
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new
            {
                error = "invalid_queue_request",
                message = exception.Message
            });
        }
        catch (InvalidOperationException exception)
        {
            return Results.Conflict(new
            {
                error = "queue_join_rejected",
                message = exception.Message
            });
        }
    });

app.MapGet(
    "/internal/v1/battle-queue/{userId:guid}/status",
    async (
        Guid userId,
        HttpRequest httpRequest,
        IBattleQueueLifecycle battleQueueLifecycle,
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
            BattleQueueStatusSnapshot status =
                await battleQueueLifecycle.GetStatusAsync(
                    userId,
                    cancellationToken);

            return Results.Ok(
                ToInternalBattleQueueStatusResponse(
                    status));
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new
            {
                error = "invalid_queue_status_request",
                message = exception.Message
            });
        }
    });

app.MapPost(
    "/internal/v1/battle-queue/{userId:guid}/leave",
    async (
        Guid userId,
        HttpRequest httpRequest,
        IBattleQueueLifecycle battleQueueLifecycle,
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
            BattleQueueLeaveSnapshot leaveResult =
                await battleQueueLifecycle.LeaveAsync(
                    userId,
                    cancellationToken);

            return Results.Ok(
                new InternalBattleQueueLeaveResponse(
                    leaveResult.HadActiveQueueEntry,
                    leaveResult.WasCancelled,
                    leaveResult.MatchAlreadyFormed,
                    ToInternalBattleQueueStatusResponse(
                        leaveResult.Status)));
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new
            {
                error = "invalid_queue_leave_request",
                message = exception.Message
            });
        }
    });

app.MapPost(
    "/internal/v1/battle-queue/{userId:guid}/connection-ticket",
    async (
        Guid userId,
        HttpRequest httpRequest,
        IBattleQueueLifecycle battleQueueLifecycle,
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
            BattleJoinTicketIssueSnapshot ticket =
                await battleQueueLifecycle
                    .IssueConnectionTicketAsync(
                        userId,
                        cancellationToken);

            return Results.Ok(
                new InternalBattleConnectionTicketResponse(
                    ticket.MatchId,
                    ticket.TicketId,
                    ticket.BattleServerPublicHost,
                    ticket.BattleServerPublicPort,
                    ticket.ExpiresAtUtc,
                    ticket.SecretBase64));
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new
            {
                error = "invalid_connection_ticket_request",
                message = exception.Message
            });
        }
        catch (InvalidOperationException exception)
        {
            return Results.Conflict(new
            {
                error = "connection_ticket_not_available",
                message = exception.Message
            });
        }
    });

if (developmentEndpointsEnabled)
{
    app.MapPost(
        "/control/v1/development/matches",
        async (
            HttpRequest httpRequest,
            DevelopmentBattleMatchCreateRequest request,
            IBattleModeCatalog battleModeCatalog,
            IBattleMatchProvisioner battleMatchProvisioner,
            CancellationToken cancellationToken) =>
        {
            if (!BattleServerControlKeyValidator.IsValid(
                    httpRequest,
                    battleServerControlKey))
            {
                return Results.Unauthorized();
            }

            try
            {
                if (!battleModeCatalog.TryGetMode(
                        request.ModeId,
                        out BattleModeDefinition? mode))
                {
                    return Results.BadRequest(new
                    {
                        error = "unknown_battle_mode",
                        message =
                            "The requested battle mode does not exist in the loaded catalog."
                    });
                }

                if (!mode.IsAvailableAt(DateTime.UtcNow))
                {
                    return Results.Conflict(new
                    {
                        error = "battle_mode_unavailable",
                        message =
                            "The requested battle mode is currently unavailable."
                    });
                }

                if (string.IsNullOrWhiteSpace(request.MapId))
                {
                    return Results.BadRequest(new
                    {
                        error = "invalid_battle_map",
                        message =
                            "A battle map id is required."
                    });
                }

                string mapId = request.MapId.Trim();

                bool mapIsPartOfModePool =
                    mode.MapPool.Any(
                        entry => string.Equals(
                            entry.MapId,
                            mapId,
                            StringComparison.OrdinalIgnoreCase));

                if (!mapIsPartOfModePool)
                {
                    return Results.BadRequest(new
                    {
                        error = "battle_map_not_allowed_for_mode",
                        message =
                            "The requested map is not part of this mode's map pool."
                    });
                }

                if (request.ExpectedPlayerCount !=
                    mode.ExpectedPlayerCount)
                {
                    return Results.BadRequest(new
                    {
                        error = "invalid_battle_player_count",
                        message =
                            $"Mode '{mode.ModeId}' requires exactly " +
                            $"{mode.ExpectedPlayerCount} players."
                    });
                }

                BattleMatchSnapshot battleMatch =
                    await battleMatchProvisioner.ProvisionAsync(
                        new CreateBattleMatchRequest(
                            mode.ModeId,
                            mode.Revision,
                            BattleModeRulesSnapshotSerializer.Serialize(
                                mode),
                            mapId,
                            mode.ExpectedPlayerCount),
                        cancellationToken);

                return Results.Created(
                    $"/control/v1/matches/{battleMatch.MatchId}",
                    ToBattleMatchResponse(battleMatch));
            }
            catch (NoReadyBattleServerSlotException)
            {
                return Results.Conflict(new
                {
                    error = "no_ready_battle_server_slot",
                    message =
                        "No fresh READY battle server slot is available."
                });
            }
            catch (InvalidOperationException exception)
            {
                return Results.Conflict(new
                {
                    error = "battle_match_provisioning_conflict",
                    message = exception.Message
                });
            }
            catch (ArgumentException exception)
            {
                return Results.BadRequest(new
                {
                    error = "invalid_development_battle_match_request",
                    message = exception.Message
                });
            } 
        });
    
    app.MapPost(
    "/control/v1/development/matches/{matchId:guid}/join-tickets",
    async (
        HttpRequest httpRequest,
        Guid matchId,
        DevelopmentBattleJoinTicketIssueRequest request,
        IBattleJoinTicketIssuer battleJoinTicketIssuer,
        CancellationToken cancellationToken) =>
    {
        if (!BattleServerControlKeyValidator.IsValid(
                httpRequest,
                battleServerControlKey))
        {
            return Results.Unauthorized();
        }

        try
        {
            BattleJoinTicketIssueSnapshot ticket =
                await battleJoinTicketIssuer.IssueAsync(
                    new IssueBattleJoinTicketRequest(
                        matchId,
                        request.UserId,
                        request.InitialVehicleId),
                    cancellationToken);

            return Results.Ok(
                new BattleJoinTicketIssueResponse(
                    ticket.TicketId,
                    ticket.MatchId,
                    ticket.MatchPlayerId,
                    ticket.BattleServerInstanceId,
                    ticket.BattleServerPublicHost,
                    ticket.BattleServerPublicPort,
                    ticket.ExpiresAtUtc,
                    ticket.SecretBase64));
        }
        catch (BattleMatchNotFoundException)
        {
            return Results.NotFound(new
            {
                error = "battle_match_not_found",
                message =
                    "The requested battle match does not exist."
            });
        }
        catch (BattleMatchNotJoinableException exception)
        {
            return Results.Conflict(new
            {
                error = "battle_match_not_joinable",
                message = exception.Message
            });
        }
        catch (NoBattleMatchPlayerSlotAvailableException exception)
        {
            return Results.Conflict(new
            {
                error = "no_battle_match_player_slot_available",
                message = exception.Message
            });
        }
        catch (BattleMatchPlayerNotEligibleForTicketException exception)
        {
            return Results.Conflict(new
            {
                error = "battle_match_player_not_eligible_for_ticket",
                message = exception.Message
            });
        }
        catch (BattleServerSlotNotFoundException)
        {
            return Results.Conflict(new
            {
                error = "battle_server_slot_not_available",
                message =
                    "The battle server slot for this match no longer exists."
            });
        }
        catch (InvalidOperationException exception)
        {
            return Results.Conflict(new
            {
                error = "battle_join_ticket_issue_conflict",
                message = exception.Message
            });
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new
            {
                error = "invalid_battle_join_ticket_request",
                message = exception.Message
            });
        }
    });
}

app.Run();

static string GetRequiredEnvironmentVariable(
    string variableName)
{
    string? value =
        Environment.GetEnvironmentVariable(
            variableName);

    if (string.IsNullOrWhiteSpace(value))
    {
        throw new InvalidOperationException(
            $"Missing environment variable '{variableName}'.");
    }

    return value.Trim();
}

static int GetRequiredPort(string variableName)
{
    string value =
        GetRequiredEnvironmentVariable(variableName);

    if (!int.TryParse(value, out int port) ||
        port is < 1 or > 65535)
    {
        throw new InvalidOperationException(
            $"Environment variable '{variableName}' must contain a valid TCP port.");
    }

    return port;
}

static int GetPositiveIntEnvironmentVariable(
    string variableName,
    int defaultValue)
{
    string? value =
        Environment.GetEnvironmentVariable(
            variableName);

    if (string.IsNullOrWhiteSpace(value))
    {
        return defaultValue;
    }

    if (!int.TryParse(value, out int parsedValue) ||
        parsedValue <= 0)
    {
        throw new InvalidOperationException(
            $"Environment variable '{variableName}' must contain a positive integer.");
    }

    return parsedValue;
}

static BattleServerSlotResponse ToResponse(
    BattleServerSlotSnapshot slot)
{
    return new BattleServerSlotResponse(
        slot.ServerInstanceId,
        slot.HostId,
        slot.Region,
        slot.PublicHost,
        slot.PublicPort,
        slot.BuildVersion,
        slot.CatalogVersion,
        ToApiStatus(slot.Status),
        slot.PlayerCount,
        slot.MaxPlayers,
        slot.ActiveMatchId,
        slot.RegisteredAtUtc,
        slot.LastHeartbeatUtc,
        slot.UpdatedAtUtc);
}

static string ToApiStatus(
    BattleServerSlotStatus status)
{
    return status switch
    {
        BattleServerSlotStatus.Ready => "ready",
        BattleServerSlotStatus.Provisioning => "provisioning",
        BattleServerSlotStatus.WaitingForPlayers =>
            "waiting_for_players",
        BattleServerSlotStatus.Warmup => "warmup",
        BattleServerSlotStatus.Running => "running",
        BattleServerSlotStatus.Finishing => "finishing",
        BattleServerSlotStatus.Resetting => "resetting",
        BattleServerSlotStatus.Offline => "offline",
        BattleServerSlotStatus.Unhealthy => "unhealthy",
        _ => throw new ArgumentOutOfRangeException(
            nameof(status),
            status,
            "Unknown battle server slot status.")
    };
}

static string ToApiCommandState(
    BattleServerCommandState state)
{
    return state switch
    {
        BattleServerCommandState.Pending => "pending",
        BattleServerCommandState.Delivered => "delivered",
        BattleServerCommandState.Acknowledged => "acknowledged",
        BattleServerCommandState.Failed => "failed",
        BattleServerCommandState.Cancelled => "cancelled",
        _ => throw new ArgumentOutOfRangeException(
            nameof(state),
            state,
            "Unknown battle server command state.")
    };
}

static BattleMatchResponse ToBattleMatchResponse(
    BattleMatchSnapshot battleMatch)
{
    return new BattleMatchResponse(
        battleMatch.MatchId,
        battleMatch.BattleServerInstanceId,
        battleMatch.ModeId,
        battleMatch.ModeRevision,
        battleMatch.MapId,
        battleMatch.ExpectedPlayerCount,
        battleMatch.Status,
        battleMatch.CreatedAtUtc,
        battleMatch.UpdatedAtUtc,
        battleMatch.WaitingForPlayersAtUtc,
        battleMatch.StartedAtUtc,
        battleMatch.FinishedAtUtc,
        battleMatch.FailureReason);
}

static bool GetEnvironmentVariableAsBoolean(
    string variableName,
    bool defaultValue)
{
    string? rawValue =
        Environment.GetEnvironmentVariable(
            variableName);

    if (string.IsNullOrWhiteSpace(rawValue))
        return defaultValue;

    if (bool.TryParse(rawValue, out bool value))
        return value;

    throw new InvalidOperationException(
        $"Environment variable '{variableName}' must be " +
        $"either 'true' or 'false'.");
}

static InternalBattleQueueStatusResponse
    ToInternalBattleQueueStatusResponse(
        BattleQueueStatusSnapshot status)
{
    ArgumentNullException.ThrowIfNull(status);

    return new InternalBattleQueueStatusResponse(
        status.HasActiveQueueEntry,
        status.QueueEntry is null
            ? null
            : ToBattleQueueEntryResponse(
                status.QueueEntry),
        status.MatchStatus,
        status.MapId,
        status.IsReadyToConnect);
}

static BattleQueueEntryResponse
    ToBattleQueueEntryResponse(
        BattleQueueEntrySnapshot queueEntry)
{
    ArgumentNullException.ThrowIfNull(queueEntry);

    return new BattleQueueEntryResponse(
        queueEntry.QueueEntryId,
        queueEntry.UserId,
        queueEntry.DeckId,
        queueEntry.ModeId,
        queueEntry.ModeRevision,
        queueEntry.InitialVehicleId,
        queueEntry.VehicleBattleRatingTenths,
        queueEntry.VehicleClass,
        queueEntry.Status,
        queueEntry.MatchId,
        queueEntry.QueuedAtUtc,
        queueEntry.UpdatedAtUtc,
        queueEntry.MatchedAtUtc,
        queueEntry.FailureReason);
}