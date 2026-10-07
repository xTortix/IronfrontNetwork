using Ironfront.AuthService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ironfront.AuthService.Persistence;

public sealed class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<RefreshSessionEntity> RefreshSessions => Set<RefreshSessionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var user = modelBuilder.Entity<UserEntity>();

        user.ToTable("users");

        user.HasKey(x => x.Id);

        user.Property(x => x.Id)
            .ValueGeneratedNever();

        user.Property(x => x.Username)
            .HasMaxLength(32)
            .IsRequired();

        user.HasIndex(x => x.NormalizedUsername)
            .IsUnique();

        user.Property(x => x.Email)
            .HasMaxLength(254)
            .IsRequired();

        user.HasIndex(x => x.NormalizedEmail)
            .IsUnique();

        user.Property(x => x.PasswordHash)
            .HasMaxLength(512)
            .IsRequired();

        user.Property(x => x.CreatedAtUtc)
            .IsRequired();

        var refreshSession = modelBuilder.Entity<RefreshSessionEntity>();

        refreshSession.ToTable("refresh_sessions");

        refreshSession.HasKey(x => x.Id);

        refreshSession.Property(x => x.Id)
            .ValueGeneratedNever();

        // SHA-256 als Hex-String: exakt 64 Zeichen.
        refreshSession.Property(x => x.TokenHash)
            .HasMaxLength(64)
            .IsRequired();

        // Derselbe rohe Token darf niemals doppelt auftauchen.
        refreshSession.HasIndex(x => x.TokenHash)
            .IsUnique();

        refreshSession.HasIndex(x => x.UserId);
        refreshSession.HasIndex(x => x.FamilyId);

        refreshSession.Property(x => x.RevocationReason)
            .HasMaxLength(64);

        refreshSession.Property(x => x.ConcurrencyStamp)
            .IsConcurrencyToken();

        refreshSession.HasOne(x => x.User)
            .WithMany(x => x.RefreshSessions)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}