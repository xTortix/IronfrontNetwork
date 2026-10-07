using Ironfront.UserService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ironfront.UserService.Infrastructure.Persistence;

public sealed class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options)
    : base(options)
    {
    }

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<UserVehicle> UserVehicles => Set<UserVehicle>();
    public DbSet<UserDeck> UserDecks => Set<UserDeck>();
    public DbSet<UserDeckSlot> UserDeckSlots => Set<UserDeckSlot>();
    public DbSet<UserTechTreeState> UserTechTreeStates => Set<UserTechTreeState>();
    public DbSet<UserResearchNode> UserResearchNodes => Set<UserResearchNode>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureUserProfiles(modelBuilder);
        ConfigureUserVehicles(modelBuilder);
        ConfigureUserDecks(modelBuilder);
        ConfigureUserDeckSlots(modelBuilder);
        ConfigureUserTechTreeStates(modelBuilder);
        ConfigureUserResearchNodes(modelBuilder);
    }

    private static void ConfigureUserProfiles(ModelBuilder modelBuilder)
    {
        var profile = modelBuilder.Entity<UserProfile>();

        profile.ToTable("user_profiles");

        profile.HasKey(x => x.UserId);

        profile.Property(x => x.UserId)
        .ValueGeneratedNever();

        profile.Property(x => x.Username)
        .HasMaxLength(64)
        .IsRequired();

        profile.Property(x => x.Email)
        .HasMaxLength(254)
        .IsRequired();

        profile.Property(x => x.CreatedAtUtc)
        .IsRequired();

        profile.Property(x => x.UpdatedAtUtc)
        .IsRequired();

        // Username/E-Mail-Unique-Checks gehören zum AuthService.
        // Der UserService speichert sie nur als Profil-Snapshot.
    }

    private static void ConfigureUserVehicles(ModelBuilder modelBuilder)
    {
        var vehicle = modelBuilder.Entity<UserVehicle>();

        vehicle.ToTable("user_vehicles");

        vehicle.HasKey(x => new
        {
            x.UserId,
            x.VehicleId
        });

        vehicle.Property(x => x.VehicleId)
        .HasMaxLength(128)
        .IsRequired();

        vehicle.Property(x => x.UnlockedAtUtc)
        .IsRequired();

        vehicle.HasIndex(x => x.VehicleId);

        vehicle.HasOne<UserProfile>()
        .WithMany()
        .HasForeignKey(x => x.UserId)
        .OnDelete(DeleteBehavior.Cascade);
    }
    
    private static void ConfigureUserDecks(ModelBuilder modelBuilder)
    {
        var deck = modelBuilder.Entity<UserDeck>();

        deck.ToTable("user_decks");

        deck.HasKey(x => x.DeckId);

        deck.Property(x => x.DeckId)
            .ValueGeneratedNever();

        deck.Property(x => x.UserId)
            .IsRequired();

        deck.Property(x => x.Name)
            .HasMaxLength(64)
            .IsRequired();

        deck.Property(x => x.Nation)
            .HasMaxLength(32)
            .IsRequired();

        deck.Property(x => x.IsActive)
            .IsRequired();

        deck.Property(x => x.CreatedAtUtc)
            .IsRequired();

        deck.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        deck.HasIndex(x => x.UserId);

        deck.HasIndex(x => x.UserId)
            .IsUnique()
            .HasFilter("\"IsActive\" = TRUE");

        deck.HasOne<UserProfile>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureUserDeckSlots(ModelBuilder modelBuilder)
    {
        var slot = modelBuilder.Entity<UserDeckSlot>();

        slot.ToTable("user_deck_slots");

        slot.HasKey(x => new
        {
            x.DeckId,
            x.SlotIndex
        });

        slot.Property(x => x.SlotIndex)
            .IsRequired();

        slot.Property(x => x.VehicleId)
            .HasMaxLength(128)
            .IsRequired();

        slot.Property(x => x.AddedAtUtc)
            .IsRequired();

        slot.HasIndex(x => new
            {
                x.DeckId,
                x.VehicleId
            })
            .IsUnique();

        slot.HasIndex(x => x.VehicleId);

        slot.HasOne<UserDeck>()
            .WithMany()
            .HasForeignKey(x => x.DeckId)
            .OnDelete(DeleteBehavior.Cascade);
    }
    
    private static void ConfigureUserTechTreeStates(
        ModelBuilder modelBuilder)
    {
        var state = modelBuilder.Entity<UserTechTreeState>();

        state.ToTable("user_tech_tree_states");

        state.HasKey(x => new
        {
            x.UserId,
            x.TechTreeId
        });

        state.Property(x => x.TechTreeId)
            .HasMaxLength(128)
            .IsRequired();

        state.Property(x => x.SelectedNodeId)
            .HasMaxLength(128);

        state.Property(x => x.CreatedAtUtc)
            .IsRequired();

        state.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        state.HasIndex(x => x.TechTreeId);

        state.HasOne<UserProfile>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureUserResearchNodes(
        ModelBuilder modelBuilder)
    {
        var researchNode = modelBuilder.Entity<UserResearchNode>();

        researchNode.ToTable("user_research_nodes");

        researchNode.HasKey(x => new
        {
            x.UserId,
            x.NodeId
        });

        researchNode.Property(x => x.NodeId)
            .HasMaxLength(128)
            .IsRequired();

        researchNode.Property(x => x.ResearchPointsApplied)
            .IsRequired();

        researchNode.Property(x => x.ResearchStartedAtUtc);

        researchNode.Property(x => x.ResearchedAtUtc);

        researchNode.Property(x => x.PurchasedAtUtc);

        researchNode.HasIndex(x => x.NodeId);

        researchNode.HasOne<UserProfile>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
