using Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Application.Contractors.Authentication;
using Persistence.Converters;
using Persistence.Configurations;

namespace Persistence.Context;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, int>
{
    private readonly ITenantProvider _tenantProvider;
    private readonly IUserContextService _userContextService;
    private readonly Guid _tenantId;
    private CurrentUser? _currentUser;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantProvider tenantProvider, IUserContextService userContextService)
        : base(options)
    {
        _tenantProvider = tenantProvider;
        _userContextService = userContextService;
        _tenantId = _tenantProvider.GetTenantId();
        // Don't call GetCurrentUser() here - will be called lazily when needed
    }

    private CurrentUser GetCurrentUser()
    {
        _currentUser ??= _userContextService.GetCurrentUser();
        return _currentUser;
    }

    public DbSet<Product> Products { get; set; }
    public DbSet<ProductCategory> ProductCategories { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<BookingDetail> BookingDetails { get; set; }
    public DbSet<Delivery> Deliveries { get; set; }
    public DbSet<DeliveryDetail> DeliveryDetails { get; set; }
    public DbSet<DeliveryChallan> DeliveryChallans { get; set; }
    public DbSet<DeliveryChallanItem> DeliveryChallanItems { get; set; }
    public DbSet<Asset> Assets { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<BaseUnit> ProductUnits { get; set; }
    public DbSet<UnitConversion> UnitConversions { get; set; }
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<Company> Companies { get; set; }
    public DbSet<Branch> Branches { get; set; }
    public DbSet<Bank> Banks { get; set; }
    public DbSet<BankTransaction> BankTransactions { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<PaymentMethod> PaymentMethods { get; set; }
    public DbSet<PrintSettings> PrintSettings { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<TransactionHead> TransactionHeads { get; set; }
    public DbSet<SalaryPayment> SalaryPayments { get; set; }
    public DbSet<RecurringChargeRun> RecurringChargeRuns { get; set; }
    public DbSet<RecurringChargeEntry> RecurringChargeEntries { get; set; }

    // legacy Users DbSet left for backward compatibility (maps to existing Users table)
    //public DbSet<User> AppUsers { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 🌍 Apply UTC DateTime converters globally to all DateTime properties
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(new UtcDateTimeValueConverter());
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(new UtcNullableDateTimeValueConverter());
                }
            }
        }

        // 🔒 Globally disable cascade deletes
        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
        }

        // Configure composite keys for Identity types
        modelBuilder.Entity<IdentityUserLogin<int>>()
               .HasKey(l => l.UserId);

        modelBuilder.Entity<IdentityUserRole<int>>()
               .HasKey(r => new { r.UserId, r.RoleId });

        modelBuilder.Entity<IdentityUserToken<int>>()
              .HasKey(r => new { r.UserId, r.Value });

        // Tenant filters and indexes (as you had them)
        modelBuilder.Entity<BaseUnit>(entity =>
        {
            entity.HasIndex(x => x.TenantId);
            if (_tenantId != Guid.Empty)
                entity.HasQueryFilter(x => x.TenantId == _tenantId);
        });

        modelBuilder.Entity<ProductCategory>(entity =>
        {
            entity.HasIndex(x => x.TenantId);
            if (_tenantId != Guid.Empty)
                entity.HasQueryFilter(x => x.TenantId == _tenantId);

            entity.HasMany(x => x.Products)
                  .WithOne(x => x.Category)
                  .HasForeignKey(x => x.CategoryId)
                  .IsRequired()
                  .OnDelete(DeleteBehavior.Restrict); // opt-in restrict
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(x => x.TenantId);
            if (_tenantId != Guid.Empty)
                entity.HasQueryFilter(x => x.TenantId == _tenantId);
        });

        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasIndex(x => x.TenantId);
            if (_tenantId != Guid.Empty)
                entity.HasQueryFilter(x => x.TenantId == _tenantId);
        });

        modelBuilder.Entity<Branch>(entity =>
        {
            entity.HasIndex(x => x.TenantId);
            if (_tenantId != Guid.Empty)
                entity.HasQueryFilter(x => x.TenantId == _tenantId);
        });

        modelBuilder.Entity<Delivery>(entity =>
        {
            entity.HasIndex(x => x.TenantId);
            if (_tenantId != Guid.Empty)
                entity.HasQueryFilter(x => x.TenantId == _tenantId && !x.IsDeleted);
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.ToTable("Transactions", "finance");
            entity.HasIndex(x => x.TenantId);
            if (_tenantId != Guid.Empty)
                entity.HasQueryFilter(x => x.TenantId == _tenantId && !x.IsDeleted);

            entity.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.DiscountAmount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.AdjustmentValue).HasColumnType("decimal(18,2)");
            entity.Property(x => x.NetAmount).HasColumnType("decimal(18,2)");

            // Configure TransactionHead foreign key
            entity.HasOne(x => x.TransactionHead)
                  .WithMany()
                  .HasForeignKey(x => x.TransactionHeadId)
                  .OnDelete(DeleteBehavior.Restrict);

            // 1-to-1 with SalaryPayment
            entity.HasOne(x => x.SalaryPayment)
                  .WithOne(sp => sp.Transaction)
                  .HasForeignKey<SalaryPayment>(sp => sp.TransactionId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SalaryPayment>(entity =>
        {
            entity.ToTable("SalaryPayments", "finance");
            entity.HasIndex(x => x.TenantId);
            entity.HasIndex(x => x.TransactionId).IsUnique();
            if (_tenantId != Guid.Empty)
                entity.HasQueryFilter(x => x.TenantId == _tenantId);

            entity.HasOne(x => x.Employee)
                  .WithMany()
                  .HasForeignKey(x => x.EmployeeId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DeliveryChallan>(entity =>
        {
            entity.HasIndex(x => x.TenantId);
            if (_tenantId != Guid.Empty)
                entity.HasQueryFilter(x => x.TenantId == _tenantId && !x.IsDeleted);

            entity.HasOne(x => x.Branch)
                  .WithMany()
                  .HasForeignKey(x => x.BranchId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.ChallanItems)
                  .WithOne(x => x.DeliveryChallan)
                  .HasForeignKey(x => x.DeliveryChallanId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DeliveryChallanItem>(entity =>
        {
            entity.HasOne(x => x.Delivery)
                  .WithMany()
                  .HasForeignKey(x => x.DeliveryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        //// Apply TransactionHead configuration
        //modelBuilder.ApplyConfiguration(new TransactionHeadConfiguration());

        // Apply DeliveryDetail configuration for cascade delete
        modelBuilder.ApplyConfiguration(new DeliveryDetailConfiguration());

        // RecurringChargeRun — immutable audit log; tenant-filtered, no soft delete
        modelBuilder.Entity<RecurringChargeRun>(entity =>
        {
            entity.HasIndex(x => x.TenantId);
            entity.HasIndex(x => x.StartedAt);
            if (_tenantId != Guid.Empty)
                entity.HasQueryFilter(x => x.TenantId == _tenantId);
        });



        modelBuilder.Entity<RecurringChargeEntry>(entity =>
        {
            entity.HasIndex(x => x.TenantId);
            if (_tenantId != Guid.Empty)
                entity.HasQueryFilter(x => x.TenantId == _tenantId);
        });
    }
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var currentUser = GetCurrentUser();

        // Handle AuditableEntity<int>
        var intEntries = ChangeTracker.Entries<AuditableEntity<int>>();
        foreach (var entry in intEntries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedTime = DateTime.UtcNow;
                    entry.Entity.CreatedById = currentUser.Id;
                    entry.Entity.TenantId = _tenantId;
                    break;

                case EntityState.Modified:
                    entry.Entity.LastUpdatedTime = DateTime.UtcNow;
                    entry.Entity.LastUpdatedById = currentUser.Id;
                    entry.Property(e => e.TenantId).IsModified = false;
                    entry.Property(e => e.CreatedById).IsModified = false;
                    entry.Property(e => e.CreatedTime).IsModified = false;
                    break;
            }
        }

        // Handle AuditableEntity<long>
        var longEntries = ChangeTracker.Entries<AuditableEntity<long>>();
        foreach (var entry in longEntries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedTime = DateTime.UtcNow;
                    entry.Entity.CreatedById = currentUser.Id;
                    entry.Entity.TenantId = _tenantId;
                    break;

                case EntityState.Modified:
                    entry.Entity.LastUpdatedTime = DateTime.UtcNow;
                    entry.Entity.LastUpdatedById = currentUser.Id;
                    entry.Property(e => e.TenantId).IsModified = false;
                    entry.Property(e => e.CreatedById).IsModified = false;
                    entry.Property(e => e.CreatedTime).IsModified = false;
                    break;
            }
        }

        // Handle plain BaseEntity<Guid> (e.g. RecurringChargeRun) — set TenantId on insert only
        var guidBaseEntries = ChangeTracker.Entries<BaseEntity<Guid>>()
            .Where(e => e.State == EntityState.Added && e.Entity is not AuditableEntity<Guid>);
        foreach (var entry in guidBaseEntries)
        {
            if (entry.Entity.TenantId == Guid.Empty)
                entry.Entity.TenantId = _tenantId;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
