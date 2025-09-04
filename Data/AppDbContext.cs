using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRMWebApp.Models;
using CRMWebApp.Services;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CRMWebApp.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        private readonly string? _currentUserId;
        private readonly bool _isAdmin;
        public AppDbContext(DbContextOptions<AppDbContext> options, IUserContext? ctx = null) : base(options)
        {
            _currentUserId = ctx?.UserId;
            _isAdmin = ctx?.IsAdmin ?? false;
        }
        public DbSet<Client> Clients => Set<Client>();
        public DbSet<Interaction> Interactions => Set<Interaction>();
        public DbSet<Deal> Deals => Set<Deal>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Client>().ToTable("Clients");
            modelBuilder.Entity<Interaction>().ToTable("Interactions");
            modelBuilder.Entity<Deal>().ToTable("Deals");
            modelBuilder.Entity<AuditLog>().ToTable("AuditLogs");

            modelBuilder.Entity<Deal>()
                .Property(d => d.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Client>()
                .HasQueryFilter(x => !x.IsDeleted && (_isAdmin || _currentUserId == null || x.UserId == _currentUserId));
            modelBuilder.Entity<Deal>()
                .HasQueryFilter(x => !x.IsDeleted && (_isAdmin || _currentUserId == null || x.UserId == _currentUserId));
            modelBuilder.Entity<Interaction>()
                .HasQueryFilter(x => !x.IsDeleted && (_isAdmin || _currentUserId == null || x.UserId == _currentUserId));

            modelBuilder.Entity<Client>().HasIndex(c => new { c.UserId, c.IsDeleted });
            modelBuilder.Entity<Deal>().HasIndex(d => new { d.UserId, d.Status, d.IsDeleted });
            modelBuilder.Entity<Interaction>().HasIndex(i => new { i.UserId, i.IsDeleted });

            modelBuilder.Entity<Client>()
                .HasIndex(c => new { c.UserId, c.Email })
                .IsUnique()
                .HasDatabaseName("IX_Clients_UserId_Email_Active")
                .HasFilter("[Email] IS NOT NULL AND [IsDeleted] = 0");

            modelBuilder.Entity<Client>()
                .HasMany(c => c.Interactions)
                .WithOne(i => i.Client!)
                .HasForeignKey(i => i.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Client>()
                .HasMany(c => c.Deals)
                .WithOne(d => d.Client!)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Interaction>()
                .HasOne(i => i.Deal)
                .WithMany()
                .HasForeignKey(i => i.DealId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Client>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Deal>()
                .HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Interaction>()
                .HasOne(i => i.User)
                .WithMany()
                .HasForeignKey(i => i.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Client>()
                .HasOne(c => c.CreatedBy)
                .WithMany()
                .HasForeignKey(c => c.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Deal>()
                .HasOne(d => d.CreatedBy)
                .WithMany()
                .HasForeignKey(d => d.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Interaction>()
                .HasOne(i => i.CreatedBy)
                .WithMany()
                .HasForeignKey(i => i.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Interaction>().Property(i => i.CreatedById).IsRequired(false);
            modelBuilder.Entity<Deal>().Property(d => d.CreatedById).IsRequired(false);
            modelBuilder.Entity<Client>().Property(c => c.CreatedById).IsRequired(false);

            modelBuilder.Entity<Interaction>().HasIndex(i => i.CreatedById);
            modelBuilder.Entity<Deal>().HasIndex(d => d.CreatedById);

            modelBuilder.Entity<Client>().Property(c => c.Email).HasMaxLength(256);

            modelBuilder.Entity<AuditLog>(b =>
            {
                b.HasKey(a => a.Id);

                b.Property(a => a.Action).IsRequired().HasMaxLength(32);

                b.HasOne(a => a.Client)
                    .WithMany()
                    .HasForeignKey(a => a.ClientId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(a => a.Deal)
                    .WithMany()
                    .HasForeignKey(a => a.DealId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(a => a.Interaction)
                    .WithMany()
                    .HasForeignKey(a => a.InteractionId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(a => a.User)
                    .WithMany()
                    .HasForeignKey(a => a.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasIndex(a => a.Timestamp);
                b.HasIndex(a => a.UserId);
                b.HasIndex(a => a.ClientId);
                b.HasIndex(a => a.DealId);
                b.HasIndex(a => a.InteractionId);

                b.ToTable(t => t.HasCheckConstraint(
                    "CK_AuditLogs_SingleTarget",
                    "(CASE WHEN [ClientId] IS NOT NULL THEN 1 ELSE 0 END +" +
                    " CASE WHEN [DealId] IS NOT NULL THEN 1 ELSE 0 END +" +
                    " CASE WHEN [InteractionId] IS NOT NULL THEN 1 ELSE 0 END) = 1"));
            });
        }

        public override int SaveChanges()
            => SaveChangesAsync(CancellationToken.None).GetAwaiter().GetResult();

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var drafts = ChangeTracker.Entries()
                .Where(e =>
                    e.Entity is not AuditLog &&
                    (e.Entity is Client || e.Entity is Deal || e.Entity is Interaction) &&
                    (e.State == EntityState.Added ||
                     e.State == EntityState.Modified ||
                     e.State == EntityState.Deleted))
                .Select(e => new { Entry = e, e.State })
                .ToList();

            var affected = await base.SaveChangesAsync(cancellationToken);

            var logs = new List<AuditLog>(drafts.Count);
            foreach (var d in drafts)
            {
                if (d.State == EntityState.Modified && !HasMeaningfulChanges(d.Entry))
                    continue;

                var action = d.State switch
                {
                    EntityState.Added => "Create",
                    EntityState.Modified => "Update",
                    EntityState.Deleted => "Delete",
                    _ => "?"
                };

                var log = new AuditLog
                {
                    Timestamp = DateTime.UtcNow,
                    UserId = _currentUserId, 
                    Action = action
                };

                var id = TryGetIntKey(d.Entry) ?? 0;

                if (d.Entry.Entity is Client) log.ClientId = id;
                else if (d.Entry.Entity is Deal) log.DealId = id;
                else if (d.Entry.Entity is Interaction) log.InteractionId = id;

                logs.Add(log);
            }

            if (logs.Count > 0)
            {
                AuditLogs.AddRange(logs);
                await base.SaveChangesAsync(cancellationToken);
            }

            return affected;
        }

        private static bool HasMeaningfulChanges(EntityEntry entry)
        {
            var ignore = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "ConcurrencyStamp", "RowVersion", "UpdatedAt"
            };

            foreach (var p in entry.Properties)
            {
                if (!p.IsModified) continue;
                if (ignore.Contains(p.Metadata.Name)) continue;

                var original = p.OriginalValue?.ToString();
                var current = p.CurrentValue?.ToString();
                if (!Equals(original, current))
                    return true;
            }
            return false;
        }

        private static int? TryGetIntKey(EntityEntry entry)
        {
            var pk = entry.Metadata.FindPrimaryKey();
            if (pk == null) return null;

            foreach (var p in pk.Properties)
            {
                var prop = entry.Property(p.Name);
                var val = prop.CurrentValue ?? prop.OriginalValue;
                if (val is int i) return i;
                if (val != null && int.TryParse(val.ToString(), out var parsed))
                    return parsed;
            }
            return null;
        }
    }
}