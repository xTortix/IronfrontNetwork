using Ironfront.Matchmaking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Ironfront.Matchmaking.Domain.Enums;



namespace Ironfront.Matchmaking.Infrastructure.Persistence;

public sealed class MatchmakingDbContext
    : DbContext
{
    public MatchmakingDbContext(
        DbContextOptions<MatchmakingDbContext> options)
        : base(options)
    {
    }

    public DbSet<BattleServerSlot> BattleServerSlots =>
        Set<BattleServerSlot>();
    
    public DbSet<BattleServerCommand> BattleServerCommands =>
        Set<BattleServerCommand>();
    
    public DbSet<BattleMatch> BattleMatches =>
        Set<BattleMatch>();
    
    public DbSet<BattleMatchPlayer> BattleMatchPlayers =>
        Set<BattleMatchPlayer>();

    public DbSet<BattleJoinTicket> BattleJoinTickets =>
        Set<BattleJoinTicket>();
    
    public DbSet<BattleQueueEntry> BattleQueueEntries =>
        Set<BattleQueueEntry>();
    
    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        ConfigureBattleServerSlots(modelBuilder);
        ConfigureBattleServerCommands(modelBuilder);
        ConfigureBattleMatches(modelBuilder);
        ConfigureBattleMatchPlayers(modelBuilder);
        ConfigureBattleJoinTickets(modelBuilder);
        ConfigureBattleQueueEntries(modelBuilder);
    }

    private static void ConfigureBattleServerSlots(
        ModelBuilder modelBuilder)
    {
        var slot = modelBuilder.Entity<BattleServerSlot>();

        slot.ToTable("battle_server_slots");

        slot.HasKey(x => x.ServerInstanceId);

        slot.Property(x => x.ServerInstanceId)
            .ValueGeneratedNever()
            .HasMaxLength(
                BattleServerSlot.MaxServerInstanceIdLength)
            .IsRequired();

        slot.Property(x => x.HostId)
            .HasMaxLength(BattleServerSlot.MaxHostIdLength)
            .IsRequired();

        slot.Property(x => x.Region)
            .HasMaxLength(BattleServerSlot.MaxRegionLength)
            .IsRequired();

        slot.Property(x => x.PublicHost)
            .HasMaxLength(BattleServerSlot.MaxPublicHostLength)
            .IsRequired();

        slot.Property(x => x.PublicPort)
            .IsRequired();

        slot.Property(x => x.BuildVersion)
            .HasMaxLength(
                BattleServerSlot.MaxBuildVersionLength)
            .IsRequired();

        slot.Property(x => x.CatalogVersion)
            .HasMaxLength(
                BattleServerSlot.MaxCatalogVersionLength)
            .IsRequired();

        slot.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        slot.Property(x => x.PlayerCount)
            .IsRequired();

        slot.Property(x => x.MaxPlayers)
            .IsRequired();

        slot.Property(x => x.ActiveMatchId);

        slot.Property(x => x.RegisteredAtUtc)
            .IsRequired();

        slot.Property(x => x.LastHeartbeatUtc)
            .IsRequired();

        slot.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        slot.HasIndex(x => x.HostId);

        slot.HasIndex(x => new
        {
            x.Region,
            x.Status,
            x.LastHeartbeatUtc
        });
    }
    
    private static void ConfigureBattleServerCommands(
        ModelBuilder modelBuilder)
    {
        var command = modelBuilder.Entity<BattleServerCommand>();

        command.ToTable("battle_server_commands");

        command.HasKey(x => x.CommandId);

        command.Property(x => x.CommandId)
            .ValueGeneratedNever();

        command.Property(x => x.BattleServerInstanceId)
            .HasMaxLength(
                BattleServerCommand.MaxBattleServerInstanceIdLength)
            .IsRequired();
        
        command.Property(x => x.MatchId);
        
        command.Property(x => x.CommandType)
            .HasMaxLength(
                BattleServerCommand.MaxCommandTypeLength)
            .IsRequired();

        command.Property(x => x.PayloadJson)
            .HasColumnType("jsonb")
            .IsRequired();

        command.Property(x => x.State)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        command.Property(x => x.CreatedAtUtc)
            .IsRequired();

        command.Property(x => x.DeliveredAtUtc);

        command.Property(x => x.AcknowledgedAtUtc);

        command.Property(x => x.FailedAtUtc);

        command.Property(x => x.FailureReason)
            .HasMaxLength(
                BattleServerCommand.MaxFailureReasonLength);

        command.HasIndex(x => new
        {
            x.BattleServerInstanceId,
            x.State,
            x.CreatedAtUtc
        });

        command.HasOne<BattleServerSlot>()
            .WithMany()
            .HasForeignKey(x => x.BattleServerInstanceId)
            .HasPrincipalKey(x => x.ServerInstanceId)
            .OnDelete(DeleteBehavior.Restrict);
        
        command.HasOne<BattleMatch>()
            .WithMany()
            .HasForeignKey(x => x.MatchId)
            .HasPrincipalKey(x => x.MatchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
    
    private static void ConfigureBattleMatches(
        ModelBuilder modelBuilder)
    {
        var battleMatch = modelBuilder.Entity<BattleMatch>();

        battleMatch.ToTable("battle_matches");

        battleMatch.HasKey(x => x.MatchId);

        battleMatch.Property(x => x.MatchId)
            .ValueGeneratedNever();

        battleMatch.Property(x => x.BattleServerInstanceId)
            .HasMaxLength(
                BattleMatch.MaxBattleServerInstanceIdLength)
            .IsRequired();

        battleMatch.Property(x => x.ModeId)
            .HasMaxLength(
                BattleMatch.MaxModeIdLength)
            .IsRequired();

        battleMatch.Property(x => x.ModeRevision)
            .IsRequired();

        battleMatch.Property(x => x.RulesSnapshotJson)
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb")
            .IsRequired();
        
        battleMatch.Property(x => x.MapId)
            .HasMaxLength(
                BattleMatch.MaxMapIdLength)
            .IsRequired();

        battleMatch.Property(x => x.ExpectedPlayerCount)
            .IsRequired();

        battleMatch.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        battleMatch.Property(x => x.CreatedAtUtc)
            .IsRequired();

        battleMatch.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        battleMatch.Property(x => x.WaitingForPlayersAtUtc);

        battleMatch.Property(x => x.StartedAtUtc);

        battleMatch.Property(x => x.FinishedAtUtc);

        battleMatch.Property(x => x.FailureReason)
            .HasMaxLength(
                BattleMatch.MaxFailureReasonLength);

        battleMatch.HasIndex(x => new
        {
            x.BattleServerInstanceId,
            x.Status,
            x.CreatedAtUtc
        });
        
        battleMatch.HasIndex(x => new
        {
            x.ModeId,
            x.ModeRevision
        });

        battleMatch.HasOne<BattleServerSlot>()
            .WithMany()
            .HasForeignKey(x => x.BattleServerInstanceId)
            .HasPrincipalKey(x => x.ServerInstanceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
    
    private static void ConfigureBattleMatchPlayers(
    ModelBuilder modelBuilder)
    {
        var matchPlayer =
            modelBuilder.Entity<BattleMatchPlayer>();

        matchPlayer.ToTable("battle_match_players");

        matchPlayer.HasKey(x => x.MatchPlayerId);

        matchPlayer.Property(x => x.MatchPlayerId)
            .ValueGeneratedNever();

        matchPlayer.Property(x => x.MatchId)
            .IsRequired();

        matchPlayer.Property(x => x.UserId)
            .IsRequired();

        matchPlayer.Property(x => x.TeamId)
            .IsRequired();

        matchPlayer.Property(x => x.TeamSlotIndex)
            .IsRequired();

        matchPlayer.Property(x => x.InitialVehicleId)
            .HasMaxLength(
                BattleMatchPlayer.MaxInitialVehicleIdLength)
            .IsRequired();

        matchPlayer.Property(x => x.PlayerSlotIndex)
            .IsRequired();

        matchPlayer.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        matchPlayer.Property(x => x.CreatedAtUtc)
            .IsRequired();

        matchPlayer.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        matchPlayer.Property(x => x.ConnectedAtUtc);

        matchPlayer.Property(x => x.DisconnectedAtUtc);

        matchPlayer.Property(x => x.LeftAtUtc);

        /*
         * Ein User darf in einem Match genau einen MatchPlayer besitzen.
         */
        matchPlayer.HasIndex(x => new
        {
            x.MatchId,
            x.UserId
        })
        .IsUnique();
        
        /*
         * Ein Teamplatz darf pro Match nur einmal vergeben werden.
         *
         * Beispiel 1v1:
         * alpha / 0
         * bravo / 0
         *
         * Beispiel 5v5:
         * alpha / 0..4
         * bravo / 0..4
         */
        matchPlayer.HasIndex(x => new
            {
                x.MatchId,
                x.TeamId,
                x.TeamSlotIndex
            })
            .IsUnique();
        
        /*
         * Spielerplatz 0, 1, 2 ... darf innerhalb eines Matches
         * nie doppelt vergeben werden.
         */
        matchPlayer.HasIndex(x => new
        {
            x.MatchId,
            x.PlayerSlotIndex
        })
        .IsUnique();

        matchPlayer.HasIndex(x => new
        {
            x.MatchId,
            x.Status,
            x.CreatedAtUtc
        });

        /*
         * Diese Alternate Key ist absichtlich vorhanden:
         * Ein Join Ticket referenziert später MatchId + MatchPlayerId
         * gemeinsam. So erzwingt PostgreSQL, dass beide IDs wirklich
         * zur selben MatchPlayer-Zeile gehören.
         */
        matchPlayer.HasAlternateKey(x => new
        {
            x.MatchId,
            x.MatchPlayerId
        });

        matchPlayer.HasOne<BattleMatch>()
            .WithMany()
            .HasForeignKey(x => x.MatchId)
            .HasPrincipalKey(x => x.MatchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
    
    private static void ConfigureBattleJoinTickets(
        ModelBuilder modelBuilder)
    {
        var ticket =
            modelBuilder.Entity<BattleJoinTicket>();

        ticket.ToTable("battle_join_tickets");

        ticket.HasKey(x => x.TicketId);

        ticket.Property(x => x.TicketId)
            .ValueGeneratedNever();

        ticket.Property(x => x.MatchId)
            .IsRequired();

        ticket.Property(x => x.MatchPlayerId)
            .IsRequired();

        ticket.Property(x => x.BattleServerInstanceId)
            .HasMaxLength(
                BattleJoinTicket.MaxBattleServerInstanceIdLength)
            .IsRequired();

        ticket.Property(x => x.SecretHash)
            .HasColumnType("bytea")
            .HasMaxLength(
                BattleJoinTicket.SecretHashLength)
            .IsRequired();

        ticket.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        ticket.Property(x => x.IssuedAtUtc)
            .IsRequired();

        ticket.Property(x => x.ExpiresAtUtc)
            .IsRequired();

        ticket.Property(x => x.ConsumedAtUtc);

        ticket.Property(x => x.ExpiredAtUtc);

        ticket.Property(x => x.RevokedAtUtc);

        /*
         * Schnell für spätere Ticket-Validierung und Cleanup:
         * Welcher Slot besitzt welche noch relevanten Tickets?
         */
        ticket.HasIndex(x => new
        {
            x.BattleServerInstanceId,
            x.Status,
            x.ExpiresAtUtc
        });

        ticket.HasIndex(x => new
        {
            x.MatchId,
            x.MatchPlayerId
        });

        ticket.HasOne<BattleMatch>()
            .WithMany()
            .HasForeignKey(x => x.MatchId)
            .HasPrincipalKey(x => x.MatchId)
            .OnDelete(DeleteBehavior.Restrict);

        /*
         * Der zusammengesetzte Foreign Key verhindert:
         *
         * Ticket.MatchId       = Match A
         * Ticket.MatchPlayerId = Spieler aus Match B
         *
         * Beides muss zur exakt selben MatchPlayer-Zeile gehören.
         */
        ticket.HasOne<BattleMatchPlayer>()
            .WithMany()
            .HasForeignKey(x => new
            {
                x.MatchId,
                x.MatchPlayerId
            })
            .HasPrincipalKey(x => new
            {
                x.MatchId,
                x.MatchPlayerId
            })
            .OnDelete(DeleteBehavior.Restrict);

        ticket.HasOne<BattleServerSlot>()
            .WithMany()
            .HasForeignKey(x => x.BattleServerInstanceId)
            .HasPrincipalKey(x => x.ServerInstanceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
    
    private static void ConfigureBattleQueueEntries(
    ModelBuilder modelBuilder)
    {
        var queueEntry =
            modelBuilder.Entity<BattleQueueEntry>();

        queueEntry.ToTable("battle_queue_entries");

        queueEntry.HasKey(x => x.QueueEntryId);

        queueEntry.Property(x => x.QueueEntryId)
            .ValueGeneratedNever();

        queueEntry.Property(x => x.UserId)
            .IsRequired();

        queueEntry.Property(x => x.DeckId)
            .IsRequired();

        queueEntry.Property(x => x.ModeId)
            .HasMaxLength(
                BattleQueueEntry.MaxModeIdLength)
            .IsRequired();

        queueEntry.Property(x => x.ModeRevision)
            .IsRequired();

        queueEntry.Property(x => x.InitialVehicleId)
            .HasMaxLength(
                BattleQueueEntry.MaxInitialVehicleIdLength)
            .IsRequired();

        queueEntry.Property(x => x.VehicleBattleRatingTenths)
            .IsRequired();

        queueEntry.Property(x => x.VehicleClass)
            .HasMaxLength(
                BattleQueueEntry.MaxVehicleClassLength)
            .IsRequired();

        queueEntry.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        queueEntry.Property(x => x.MatchId);

        queueEntry.Property(x => x.QueuedAtUtc)
            .IsRequired();

        queueEntry.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        queueEntry.Property(x => x.MatchedAtUtc);

        queueEntry.Property(x => x.CancelledAtUtc);

        queueEntry.Property(x => x.FailedAtUtc);

        queueEntry.Property(x => x.CompletedAtUtc);

        queueEntry.Property(x => x.FailureReason)
            .HasMaxLength(
                BattleQueueEntry.MaxFailureReasonLength);

        /*
         * Das ist der zentrale Query-Index für den späteren Worker:
         *
         * „Gib mir wartende Spieler in Modus X / Revision Y,
         * sortiert nach BR und Queue-Zeit.“
         */
        queueEntry.HasIndex(x => new
        {
            x.ModeId,
            x.ModeRevision,
            x.Status,
            x.VehicleBattleRatingTenths,
            x.QueuedAtUtc
        });

        queueEntry.HasIndex(x => x.MatchId);

        /*
         * Ein User darf nicht parallel in mehreren aktiven
         * Queue-/Match-Zuständen stehen.
         *
         * Queued  = sucht noch
         * Matched = bereits einem Match zugeordnet
         *
         * Sobald der Match später endet, wird der Queue-Eintrag
         * auf Completed gesetzt und ein neuer Queue-Beitritt ist möglich.
         */
        queueEntry.HasIndex(x => x.UserId)
            .IsUnique()
            .HasFilter(
                "\"Status\" IN ('Queued', 'Matched')");

        queueEntry.HasOne<BattleMatch>()
            .WithMany()
            .HasForeignKey(x => x.MatchId)
            .HasPrincipalKey(x => x.MatchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}