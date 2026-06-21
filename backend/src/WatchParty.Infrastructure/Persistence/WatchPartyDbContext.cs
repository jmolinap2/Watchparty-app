using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WatchParty.Application.Abstractions.Auditing;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Domain.Admin;
using WatchParty.Domain.Chat;
using WatchParty.Domain.Common;
using WatchParty.Domain.Identity;
using WatchParty.Domain.Playback;
using WatchParty.Domain.Reports;
using WatchParty.Domain.Rooms;
using WatchParty.Domain.Users;

namespace WatchParty.Infrastructure.Persistence;

public sealed class WatchPartyDbContext(
    DbContextOptions<WatchPartyDbContext> options,
    IAuditContextAccessor auditContextAccessor) : DbContext(options), IUnitOfWork
{
    private static readonly JsonSerializerOptions AuditJsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly HashSet<string> AuditMetadataNames =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "CreatedAtUtc",
            "UpdatedAtUtc",
            "LastModificationTime",
            "LastModifiedAtUtc"
        };

    private static readonly string[] SensitiveNameParts =
    [
        "password",
        "tokenhash",
        "replacedbytokenhash",
        "securitystamp",
        "secret",
        "credential"
    ];

    public DbSet<AllowedDomain> AllowedDomains => Set<AllowedDomain>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
    public DbSet<UserBlock> UserBlocks => Set<UserBlock>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<RoomMember> RoomMembers => Set<RoomMember>();
    public DbSet<MediaItem> MediaItems => Set<MediaItem>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<Report> Reports => Set<Report>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ChangeTracker.DetectChanges();

        var now = DateTimeOffset.UtcNow;
        ApplyAutomaticAuditMetadata(now);

        var dataAuditLogs = BuildDataAuditLogs();
        if (dataAuditLogs.Count > 0)
        {
            await AuditLogs.AddRangeAsync(dataAuditLogs, cancellationToken);
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureAdmin(modelBuilder);
        ConfigureIdentity(modelBuilder);
        ConfigureRooms(modelBuilder);
        ConfigurePlayback(modelBuilder);
        ConfigureChat(modelBuilder);
        ConfigureReports(modelBuilder);

        IgnoreDomainEvents(modelBuilder);
    }

    private static void ConfigureAdmin(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AllowedDomain>(builder =>
        {
            builder.ToTable("allowed_domains");
            builder.HasKey(domain => domain.Id);
            builder.Property(domain => domain.Host).HasMaxLength(255).IsRequired();
            builder.HasIndex(domain => domain.Host).IsUnique();
        });

        modelBuilder.Entity<AuditLog>(builder =>
        {
            builder.ToTable("audit_logs");
            builder.HasKey(log => log.Id);
            builder.Property(log => log.Category).HasConversion<string>().HasMaxLength(32).IsRequired();
            builder.Property(log => log.Action).HasMaxLength(160).IsRequired();
            builder.Property(log => log.TargetType).HasMaxLength(120);
            builder.Property(log => log.TargetId).HasMaxLength(120);
            builder.Property(log => log.IpAddress).HasMaxLength(64);
            builder.Property(log => log.Resource).HasMaxLength(180);
            builder.Property(log => log.Operation).HasMaxLength(180);
            builder.Property(log => log.HttpMethod).HasMaxLength(16);
            builder.Property(log => log.RequestPath).HasMaxLength(512);
            builder.Property(log => log.UserAgent).HasMaxLength(512);
            builder.Property(log => log.CorrelationId).HasMaxLength(120);
            builder.HasIndex(log => log.CreatedAtUtc);
            builder.HasIndex(log => new { log.Category, log.CreatedAtUtc });
            builder.HasIndex(log => log.ActorUserId);
            builder.HasIndex(log => log.CorrelationId);
        });
    }

    private static void ConfigureIdentity(ModelBuilder modelBuilder)
    {
        var emailComparer = new ValueComparer<Email>(
            (left, right) => left != null && right != null && left.Value == right.Value,
            email => email.Value.GetHashCode(StringComparison.Ordinal),
            email => Email.FromTrusted(email.Value));

        modelBuilder.Entity<User>(builder =>
        {
            builder.ToTable("users");
            builder.HasKey(user => user.Id);
            builder.Property(user => user.Email)
                .HasConversion(email => email.Value, value => Email.FromTrusted(value))
                .Metadata.SetValueComparer(emailComparer);
            builder.Property(user => user.Email).HasMaxLength(Email.MaxLength).IsRequired();
            builder.HasIndex(user => user.Email).IsUnique();
            builder.Property(user => user.PasswordHash).HasMaxLength(512).IsRequired();
            builder.Property(user => user.DisplayName).HasMaxLength(User.MaxDisplayNameLength).IsRequired();
            builder.Property(user => user.AvatarUrl).HasMaxLength(2048);
            builder.Property(user => user.Role).HasConversion<string>().HasMaxLength(32).IsRequired();
            builder.Property(user => user.BlockedReason).HasMaxLength(1000);
        });

        modelBuilder.Entity<RefreshToken>(builder =>
        {
            builder.ToTable("refresh_tokens");
            builder.HasKey(token => token.Id);
            builder.Property(token => token.TokenHash).HasMaxLength(512).IsRequired();
            builder.Property(token => token.ReplacedByTokenHash).HasMaxLength(512);
            builder.Property(token => token.CreatedByIp).HasMaxLength(64);
            builder.HasIndex(token => token.TokenHash).IsUnique();
            builder.HasIndex(token => token.UserId);
        });

        modelBuilder.Entity<PasswordResetToken>(builder =>
        {
            builder.ToTable("password_reset_tokens");
            builder.HasKey(token => token.Id);
            builder.Property(token => token.TokenHash).HasMaxLength(512).IsRequired();
            builder.HasIndex(token => token.TokenHash).IsUnique();
            builder.HasIndex(token => token.UserId);
        });

        modelBuilder.Entity<EmailVerificationToken>(builder =>
        {
            builder.ToTable("email_verification_tokens");
            builder.HasKey(token => token.Id);
            builder.Property(token => token.TokenHash).HasMaxLength(512).IsRequired();
            builder.HasIndex(token => token.TokenHash).IsUnique();
            builder.HasIndex(token => token.UserId);
        });

        modelBuilder.Entity<UserBlock>(builder =>
        {
            builder.ToTable("user_blocks");
            builder.HasKey(block => block.Id);
            builder.HasIndex(block => new { block.BlockerUserId, block.BlockedUserId }).IsUnique();
        });
    }

    private static void ConfigureRooms(ModelBuilder modelBuilder)
    {
        var roomCodeComparer = new ValueComparer<RoomCode>(
            (left, right) => left != null && right != null && left.Value == right.Value,
            code => code.Value.GetHashCode(StringComparison.Ordinal),
            code => RoomCode.FromTrusted(code.Value));

        modelBuilder.Entity<Room>(builder =>
        {
            builder.ToTable("rooms");
            builder.HasKey(room => room.Id);
            builder.Property(room => room.Name).HasMaxLength(Room.MaxNameLength).IsRequired();
            builder.Property(room => room.Code)
                .HasConversion(code => code.Value, value => RoomCode.FromTrusted(value))
                .Metadata.SetValueComparer(roomCodeComparer);
            builder.Property(room => room.Code).HasMaxLength(RoomCode.Length).IsRequired();
            builder.HasIndex(room => room.Code).IsUnique();
            builder.Property(room => room.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            builder.OwnsOne(room => room.Settings, ConfigureRoomSettings);
            // ActiveMembers is a computed view over _members; without this EF maps it
            // as a second navigation and emits a shadow RoomId1 FK on room_members.
            builder.Ignore(room => room.ActiveMembers);
            builder.HasMany(room => room.Members)
                .WithOne()
                .HasForeignKey(member => member.RoomId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Navigation(room => room.Members).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.HasIndex(room => room.HostUserId);
        });

        modelBuilder.Entity<RoomMember>(builder =>
        {
            builder.ToTable("room_members");
            builder.HasKey(member => member.Id);
            builder.Property(member => member.Role).HasConversion<string>().HasMaxLength(32).IsRequired();
            builder.HasIndex(member => new { member.RoomId, member.UserId });
        });
    }

    private static void ConfigurePlayback(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MediaItem>(builder =>
        {
            builder.ToTable("media_items");
            builder.HasKey(media => media.Id);
            builder.Property(media => media.Title).HasMaxLength(MediaItem.MaxTitleLength).IsRequired();
            builder.OwnsOne(media => media.Source, ConfigureMediaSource);
            builder.HasIndex(media => media.RoomId);
        });
    }

    private static void ConfigureChat(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChatMessage>(builder =>
        {
            builder.ToTable("chat_messages");
            builder.HasKey(message => message.Id);
            builder.Property(message => message.Content).HasMaxLength(ChatMessage.MaxLength).IsRequired();
            builder.HasIndex(message => new { message.RoomId, message.CreatedAtUtc });
        });
    }

    private static void ConfigureReports(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Report>(builder =>
        {
            builder.ToTable("reports");
            builder.HasKey(report => report.Id);
            builder.Property(report => report.Type).HasConversion<string>().HasMaxLength(32).IsRequired();
            builder.Property(report => report.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            builder.Property(report => report.Reason).HasMaxLength(Report.MaxReasonLength).IsRequired();
            builder.Property(report => report.ResolutionNote).HasMaxLength(1000);
            builder.HasIndex(report => report.Status);
            builder.HasIndex(report => report.TargetUserId);
            builder.HasIndex(report => report.TargetMessageId);
        });
    }

    private static void ConfigureRoomSettings(OwnedNavigationBuilder<Room, RoomSettings> builder)
    {
        builder.Property(settings => settings.IsPrivate).HasColumnName("is_private");
        builder.Property(settings => settings.MaxMembers).HasColumnName("max_members");
    }

    private static void ConfigureMediaSource(OwnedNavigationBuilder<MediaItem, MediaSource> builder)
    {
        builder.Property(source => source.Kind)
            .HasColumnName("source_kind")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(source => source.Url).HasColumnName("source_url").HasMaxLength(2048).IsRequired();
        builder.Property(source => source.OriginalUrl).HasColumnName("source_original_url").HasMaxLength(2048);
        builder.Property(source => source.ProviderId).HasColumnName("source_provider_id").HasMaxLength(255);
    }

    private static void IgnoreDomainEvents(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(AggregateRoot).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType).Ignore(nameof(AggregateRoot.DomainEvents));
            }
        }
    }

    private void ApplyAutomaticAuditMetadata(DateTimeOffset now)
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            if (entry.State == EntityState.Added)
            {
                SetDateTimeOffsetProperty(entry, "CreatedAtUtc", now, onlyWhenDefault: true);
            }

            if (entry.State == EntityState.Modified)
            {
                SetDateTimeOffsetProperty(entry, "UpdatedAtUtc", now, onlyWhenDefault: false);
            }
        }
    }

    private IReadOnlyCollection<AuditLog> BuildDataAuditLogs()
    {
        var context = auditContextAccessor.Current;
        var entries = ChangeTracker.Entries()
            .Where(entry => entry.Entity is Entity and not AuditLog)
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        var logs = new List<AuditLog>(entries.Count);
        foreach (var entry in entries)
        {
            var changes = GetPropertyChanges(entry);
            if (changes.Count == 0)
            {
                continue;
            }

            var entityName = entry.Metadata.ClrType.Name;
            var operation = entry.State switch
            {
                EntityState.Added => "Created",
                EntityState.Modified => "Updated",
                EntityState.Deleted => "Deleted",
                _ => entry.State.ToString()
            };

            var details = JsonSerializer.Serialize(
                new
                {
                    state = entry.State.ToString(),
                    changes
                },
                AuditJsonOptions);

            logs.Add(AuditLog.DataChange(
                $"{entityName}.{operation}",
                context.ActorUserId,
                entityName,
                GetTargetId(entry),
                details,
                context.IpAddress,
                context.CorrelationId));
        }

        return logs;
    }

    private static Dictionary<string, object?> GetPropertyChanges(EntityEntry entry)
    {
        var changes = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var property in entry.Properties)
        {
            var propertyName = property.Metadata.Name;
            if (property.Metadata.IsPrimaryKey()
                || property.Metadata.IsShadowProperty()
                || AuditMetadataNames.Contains(propertyName))
            {
                continue;
            }

            if (entry.State == EntityState.Added)
            {
                changes[propertyName] = RedactIfSensitive(propertyName, property.CurrentValue);
            }
            else if (entry.State == EntityState.Deleted)
            {
                changes[propertyName] = RedactIfSensitive(propertyName, property.OriginalValue);
            }
            else if (property.IsModified && !Equals(property.OriginalValue, property.CurrentValue))
            {
                changes[propertyName] = new
                {
                    from = RedactIfSensitive(propertyName, property.OriginalValue),
                    to = RedactIfSensitive(propertyName, property.CurrentValue)
                };
            }
        }

        return changes;
    }

    private static object? RedactIfSensitive(string propertyName, object? value)
    {
        return SensitiveNameParts.Any(part => propertyName.Contains(part, StringComparison.OrdinalIgnoreCase))
            ? "***REDACTED***"
            : value;
    }

    private static string GetTargetId(EntityEntry entry)
    {
        var idProperty = entry.Metadata.FindProperty(nameof(Entity.Id));
        if (idProperty is null)
        {
            return string.Empty;
        }

        var property = entry.Property(idProperty.Name);
        var value = entry.State == EntityState.Deleted ? property.OriginalValue : property.CurrentValue;
        return value?.ToString() ?? string.Empty;
    }

    private static void SetDateTimeOffsetProperty(
        EntityEntry entry,
        string propertyName,
        DateTimeOffset value,
        bool onlyWhenDefault)
    {
        if (entry.Metadata.FindProperty(propertyName) is null)
        {
            return;
        }

        var property = entry.Property(propertyName);
        if (onlyWhenDefault && property.CurrentValue is DateTimeOffset current && current != default)
        {
            return;
        }

        property.CurrentValue = value;
    }
}
