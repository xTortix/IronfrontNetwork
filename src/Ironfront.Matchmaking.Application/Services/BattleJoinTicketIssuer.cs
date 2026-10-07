using System.Security.Cryptography;
using Ironfront.Matchmaking.Application.Configuration;
using Ironfront.Matchmaking.Application.Exceptions;
using Ironfront.Matchmaking.Application.Models;
using Ironfront.Matchmaking.Domain.Entities;
using Ironfront.Matchmaking.Domain.Enums;

namespace Ironfront.Matchmaking.Application.Services;

public sealed class BattleJoinTicketIssuer
    : IBattleJoinTicketIssuer
{
    private const int SecretByteLength = 32;

    private readonly IBattleMatchRepository matchRepository;
    private readonly IBattleMatchPlayerRepository matchPlayerRepository;
    private readonly IBattleJoinTicketRepository ticketRepository;
    private readonly IBattleServerSlotRepository slotRepository;
    private readonly IMatchmakingUnitOfWork unitOfWork;
    private readonly BattleJoinTicketPolicy ticketPolicy;
    private readonly TimeProvider timeProvider;

    public BattleJoinTicketIssuer(
        IBattleMatchRepository matchRepository,
        IBattleMatchPlayerRepository matchPlayerRepository,
        IBattleJoinTicketRepository ticketRepository,
        IBattleServerSlotRepository slotRepository,
        IMatchmakingUnitOfWork unitOfWork,
        BattleJoinTicketPolicy ticketPolicy,
        TimeProvider timeProvider)
    {
        this.matchRepository = matchRepository
            ?? throw new ArgumentNullException(
                nameof(matchRepository));

        this.matchPlayerRepository = matchPlayerRepository
            ?? throw new ArgumentNullException(
                nameof(matchPlayerRepository));

        this.ticketRepository = ticketRepository
            ?? throw new ArgumentNullException(
                nameof(ticketRepository));

        this.slotRepository = slotRepository
            ?? throw new ArgumentNullException(
                nameof(slotRepository));

        this.unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(
                nameof(unitOfWork));

        this.ticketPolicy = ticketPolicy
            ?? throw new ArgumentNullException(
                nameof(ticketPolicy));

        this.timeProvider = timeProvider
            ?? throw new ArgumentNullException(
                nameof(timeProvider));
    }

    public Task<BattleJoinTicketIssueSnapshot> IssueAsync(
        IssueBattleJoinTicketRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.MatchId == Guid.Empty)
        {
            throw new ArgumentException(
                "Match id must not be empty.",
                nameof(request));
        }

        if (request.UserId == Guid.Empty)
        {
            throw new ArgumentException(
                "User id must not be empty.",
                nameof(request));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(
            request.InitialVehicleId);

        return unitOfWork.ExecuteInTransactionAsync(
            async transactionCancellationToken =>
            {
                BattleMatch? battleMatch =
                    await matchRepository.FindByIdForUpdateAsync(
                        request.MatchId,
                        transactionCancellationToken);

                if (battleMatch is null)
                {
                    throw new BattleMatchNotFoundException(
                        request.MatchId);
                }

                if (battleMatch.Status !=
                    BattleMatchStatus.WaitingForPlayers)
                {
                    throw new BattleMatchNotJoinableException(
                        battleMatch.MatchId,
                        battleMatch.Status.ToString());
                }

                BattleServerSlot? slot =
                    await slotRepository
                        .FindByServerInstanceIdAsync(
                            battleMatch.BattleServerInstanceId,
                            transactionCancellationToken);

                if (slot is null)
                {
                    throw new BattleServerSlotNotFoundException(
                        battleMatch.BattleServerInstanceId);
                }

                if (slot.Status !=
                    BattleServerSlotStatus.WaitingForPlayers ||
                    slot.ActiveMatchId != battleMatch.MatchId)
                {
                    throw new InvalidOperationException(
                        $"Battle server slot '{slot.ServerInstanceId}' is not ready for match '{battleMatch.MatchId}'.");
                }

                BattleMatchPlayer? matchPlayer =
                    await matchPlayerRepository
                        .FindByMatchAndUserAsync(
                            battleMatch.MatchId,
                            request.UserId,
                            transactionCancellationToken);

                if (matchPlayer is null)
                {
                    throw new InvalidOperationException(
                        $"User '{request.UserId:D}' is not reserved " +
                        $"for battle match '{battleMatch.MatchId:D}'.");
                }

                if (!matchPlayer.CanReceiveJoinTicket)
                {
                    throw new BattleMatchPlayerNotEligibleForTicketException(
                        matchPlayer.MatchPlayerId,
                        matchPlayer.Status.ToString());
                }

                DateTime utcNow =
                    timeProvider.GetUtcNow().UtcDateTime;

                IReadOnlyList<BattleJoinTicket> oldIssuedTickets =
                    await ticketRepository
                        .GetIssuedForMatchPlayerForUpdateAsync(
                            matchPlayer.MatchPlayerId,
                            transactionCancellationToken);

                foreach (BattleJoinTicket oldTicket in oldIssuedTickets)
                {
                    oldTicket.Revoke(utcNow);
                }

                byte[] secret =
                    RandomNumberGenerator.GetBytes(
                        SecretByteLength);

                byte[] secretHash =
                    SHA256.HashData(secret);

                DateTime expiresAtUtc =
                    utcNow + ticketPolicy.Lifetime;

                BattleJoinTicket ticket =
                    BattleJoinTicket.Issue(
                        battleMatch.MatchId,
                        matchPlayer.MatchPlayerId,
                        slot.ServerInstanceId,
                        secretHash,
                        utcNow,
                        expiresAtUtc);

                ticketRepository.Add(ticket);

                return new BattleJoinTicketIssueSnapshot(
                    ticket.TicketId,
                    battleMatch.MatchId,
                    matchPlayer.MatchPlayerId,
                    slot.ServerInstanceId,
                    slot.PublicHost,
                    slot.PublicPort,
                    expiresAtUtc,
                    Convert.ToBase64String(secret));
            },
            cancellationToken);
    }
}