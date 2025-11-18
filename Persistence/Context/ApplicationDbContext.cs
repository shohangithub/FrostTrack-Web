using Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Application.Contractors.Authentication;

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
    public DbSet<Purchase> Purchases { get; set; }
    public DbSet<PurchaseDetail> PurchaseDetails { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<BookingDetail> BookingDetails { get; set; }
    public DbSet<Delivery> Deliveries { get; set; }
    public DbSet<DeliveryDetail> DeliveryDetails { get; set; }
    public DbSet<SupplierPayment> SupplierPayments { get; set; }
    public DbSet<SupplierPaymentDetail> SupplierPaymentDetails { get; set; }
    public DbSet<Sales> Sales { get; set; }
    public DbSet<SalesDetail> SalesDetails { get; set; }
    public DbSet<SaleReturn> SaleReturns { get; set; }
    public DbSet<SaleReturnDetail> SaleReturnDetails { get; set; }
    public DbSet<Damage> Damages { get; set; }
    public DbSet<Asset> Assets { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<Stock> Stocks { get; set; }
    public DbSet<BaseUnit> ProductUnits { get; set; }
    public DbSet<UnitConversion> UnitConversions { get; set; }
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<Company> Companies { get; set; }
    public DbSet<Branch> Branches { get; set; }
    public DbSet<Bank> Banks { get; set; }
    public DbSet<BankTransaction> BankTransactions { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<PaymentMethod> PaymentMethods { get; set; }
    public DbSet<PrintSettings> PrintSettings { get; set; }
    // legacy Users DbSet left for backward compatibility (maps to existing Users table)
    //public DbSet<User> AppUsers { get; set; }

    // protected override void OnModelCreating(ModelBuilder modelBuilder)
    // {
    //     //builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

    //     //base.OnModelCreating(builder);

    //     // Global fix for all audit field cascade conflicts - SIMPLE & EFFECTIVE
    //     //modelBuilder.Entity<Product>().HasOne<User>().WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.NoAction);
    //     //modelBuilder.Entity<Product>().HasOne<User>().WithMany().HasForeignKey(x => x.LastUpdatedById).OnDelete(DeleteBehavior.NoAction);
    //     //modelBuilder.Entity<ProductCategory>().HasOne<User>().WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.NoAction);
    //     //modelBuilder.Entity<ProductCategory>().HasOne<User>().WithMany().HasForeignKey(x => x.LastUpdatedById).OnDelete(DeleteBehavior.NoAction);
    //     //modelBuilder.Entity<BaseUnit>().HasOne<User>().WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.NoAction);
    //     //modelBuilder.Entity<BaseUnit>().HasOne<User>().WithMany().HasForeignKey(x => x.LastUpdatedById).OnDelete(DeleteBehavior.NoAction);
    //     //modelBuilder.Entity<UnitConversion>().HasOne<User>().WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.NoAction);
    //     //modelBuilder.Entity<UnitConversion>().HasOne<User>().WithMany().HasForeignKey(x => x.LastUpdatedById).OnDelete(DeleteBehavior.NoAction);
    //     //modelBuilder.Entity<Purchase>().HasOne<User>().WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.NoAction);
    //     //modelBuilder.Entity<Purchase>().HasOne<User>().WithMany().HasForeignKey(x => x.LastUpdatedById).OnDelete(DeleteBehavior.NoAction);
    //     //modelBuilder.Entity<PurchaseDetail>().HasOne<User>().WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.NoAction);
    //     //modelBuilder.Entity<PurchaseDetail>().HasOne<User>().WithMany().HasForeignKey(x => x.LastUpdatedById).OnDelete(DeleteBehavior.NoAction);
    //     //modelBuilder.Entity<Sales>().HasOne<User>().WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.NoAction);
    //     //modelBuilder.Entity<Sales>().HasOne<User>().WithMany().HasForeignKey(x => x.LastUpdatedById).OnDelete(DeleteBehavior.NoAction);
    //     //modelBuilder.Entity<SalesDetail>().HasOne<User>().WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.NoAction);
    //     //modelBuilder.Entity<SalesDetail>().HasOne<User>().WithMany().HasForeignKey(x => x.LastUpdatedById).OnDelete(DeleteBehavior.NoAction);
    //     //modelBuilder.Entity<Stock>().HasOne<User>().WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.NoAction);
    //     //modelBuilder.Entity<Stock>().HasOne<User>().WithMany().HasForeignKey(x => x.LastUpdatedById).OnDelete(DeleteBehavior.NoAction);





    //     //modelBuilder.Entity<User>(entity =>
    //     //{
    //     //    entity.HasIndex(x => x.TenantId);
    //     //    if (_tenantId != Guid.Empty)
    //     //        entity.HasQueryFilter(x => x.TenantId == _tenantId);


    //     //    // entity.HasMany(x => x.ProductsCreated).WithOne(x => x.CreatedBy).HasForeignKey(x => x.CreatedById).IsRequired().OnDelete(DeleteBehavior.Restrict);
    //     //    // entity.HasMany(x => x.ProductsUpdated).WithOne(x => x.LastUpdatedBy).HasForeignKey(x => x.LastUpdatedById).IsRequired(false).OnDelete(DeleteBehavior.Restrict);

    //     //    // entity.HasMany(x => x.BaseUnitsCreated).WithOne(x => x.CreatedBy).HasForeignKey(x => x.CreatedById).IsRequired().OnDelete(DeleteBehavior.Restrict);
    //     //    // entity.HasMany(x => x.BaseUnitsUpdated).WithOne(x => x.LastUpdatedBy).HasForeignKey(x => x.LastUpdatedById).IsRequired(false).OnDelete(DeleteBehavior.Restrict);

    //     //    // entity.HasMany(x => x.UnitConversionsCreated).WithOne(x => x.CreatedBy).HasForeignKey(x => x.CreatedById).IsRequired().OnDelete(DeleteBehavior.Restrict);
    //     //    // entity.HasMany(x => x.UnitConversionsUpdated).WithOne(x => x.LastUpdatedBy).HasForeignKey(x => x.LastUpdatedById).IsRequired(false).OnDelete(DeleteBehavior.Restrict);


    //     //    // entity.HasMany(x => x.ProductCategoriesCreated).WithOne(x => x.CreatedBy).HasForeignKey(x => x.CreatedById).IsRequired().OnDelete(DeleteBehavior.Restrict);
    //     //    // entity.HasMany(x => x.ProductCategoriesUpdated).WithOne(x => x.LastUpdatedBy).HasForeignKey(x => x.LastUpdatedById).IsRequired(false).OnDelete(DeleteBehavior.Restrict);

    //     //    // entity.HasMany(x => x.PurchasesCreated).WithOne(x => x.CreatedBy).HasForeignKey(x => x.CreatedById).IsRequired().OnDelete(DeleteBehavior.Restrict);
    //     //    // entity.HasMany(x => x.PurchasesUpdated).WithOne(x => x.LastUpdatedBy).HasForeignKey(x => x.LastUpdatedById).IsRequired(false).OnDelete(DeleteBehavior.Restrict);

    //     //    // entity.HasMany(x => x.PurchaseDetailsCreated).WithOne(x => x.CreatedBy).HasForeignKey(x => x.CreatedById).IsRequired().OnDelete(DeleteBehavior.Restrict);
    //     //    // entity.HasMany(x => x.PurchaseDetailsUpdated).WithOne(x => x.LastUpdatedBy).HasForeignKey(x => x.LastUpdatedById).IsRequired(false).OnDelete(DeleteBehavior.Restrict);

    //     //    // entity.HasMany(x => x.SalesCreated).WithOne(x => x.CreatedBy).HasForeignKey(x => x.CreatedById).IsRequired().OnDelete(DeleteBehavior.Restrict);
    //     //    // entity.HasMany(x => x.SalesUpdated).WithOne(x => x.LastUpdatedBy).HasForeignKey(x => x.LastUpdatedById).IsRequired(false).OnDelete(DeleteBehavior.Restrict);

    //     //    // entity.HasMany(x => x.SalesDetailsCreated).WithOne(x => x.CreatedBy).HasForeignKey(x => x.CreatedById).IsRequired().OnDelete(DeleteBehavior.Restrict);
    //     //    // entity.HasMany(x => x.SalesDetailsUpdated).WithOne(x => x.LastUpdatedBy).HasForeignKey(x => x.LastUpdatedById).IsRequired(false).OnDelete(DeleteBehavior.Restrict);

    //     //    // entity.HasMany(x => x.StocksCreated).WithOne(x => x.CreatedBy).HasForeignKey(x => x.CreatedById).IsRequired().OnDelete(DeleteBehavior.Restrict);
    //     //    // entity.HasMany(x => x.StocksUpdated).WithOne(x => x.LastUpdatedBy).HasForeignKey(x => x.LastUpdatedById).IsRequired(false).OnDelete(DeleteBehavior.Restrict);

    //     //});

    //     modelBuilder.Entity<BaseUnit>(entity =>
    //     {
    //         entity.HasIndex(x => x.TenantId);
    //         if (_tenantId != Guid.Empty)
    //             entity.HasQueryFilter(x => x.TenantId == _tenantId);
    //     });

    //     modelBuilder.Entity<ProductCategory>(entity =>
    //     {
    //         entity.HasIndex(x => x.TenantId);
    //         if (_tenantId != Guid.Empty)
    //             entity.HasQueryFilter(x => x.TenantId == _tenantId);



    //         entity.HasMany(x => x.Products).WithOne(x => x.Category).HasForeignKey(x => x.CategoryId).IsRequired().OnDelete(DeleteBehavior.Restrict);

    //     });

    //     modelBuilder.Entity<Product>(entity =>
    //     {
    //         entity.HasIndex(x => x.TenantId);
    //         if (_tenantId != Guid.Empty)
    //             entity.HasQueryFilter(x => x.TenantId == _tenantId);

    //     });

    //     modelBuilder.Entity<Purchase>(entity =>
    //     {
    //         entity.HasIndex(x => x.TenantId);
    //         if (_tenantId != Guid.Empty)
    //             entity.HasQueryFilter(x => x.TenantId == _tenantId);

    //     });

    //     modelBuilder.Entity<PurchaseDetail>(entity =>
    //     {
    //         entity.HasIndex(x => x.TenantId);
    //         if (_tenantId != Guid.Empty)
    //             entity.HasQueryFilter(x => x.TenantId == _tenantId);

    //     });

    //     modelBuilder.Entity<Sales>(entity =>
    //     {
    //         entity.HasIndex(x => x.TenantId);
    //         if (_tenantId != Guid.Empty)
    //             entity.HasQueryFilter(x => x.TenantId == _tenantId);

    //     });

    //     modelBuilder.Entity<SalesDetail>(entity =>
    //     {
    //         entity.HasIndex(x => x.TenantId);
    //         if (_tenantId != Guid.Empty)
    //             entity.HasQueryFilter(x => x.TenantId == _tenantId);

    //     });

    //     modelBuilder.Entity<Stock>(entity =>
    //     {
    //         entity.HasIndex(x => x.TenantId);
    //         if (_tenantId != Guid.Empty)
    //             entity.HasQueryFilter(x => x.TenantId == _tenantId);

    //     });

    //     modelBuilder.Entity<Company>(entity =>
    //     {
    //         entity.HasIndex(x => x.TenantId);
    //         entity.HasQueryFilter(x => x.TenantId == _tenantId);
    //     });



    //     modelBuilder.Entity<SaleReturn>(entity =>
    //     {
    //         entity.HasIndex(x => x.TenantId);
    //         if (_tenantId != Guid.Empty)
    //             entity.HasQueryFilter(x => x.TenantId == _tenantId);

    //     });

    //     modelBuilder.Entity<SaleReturnDetail>(entity =>
    //     {
    //         entity.HasIndex(x => x.TenantId);
    //         if (_tenantId != Guid.Empty)
    //             entity.HasQueryFilter(x => x.TenantId == _tenantId);

    //     });




    //     modelBuilder.Entity<Branch>(entity =>
    //     {
    //         entity.HasIndex(x => x.TenantId);
    //         if (_tenantId != Guid.Empty)
    //             entity.HasQueryFilter(x => x.TenantId == _tenantId);

    //         entity.HasMany(x => x.Purchases).WithOne(x => x.Branch).HasForeignKey(x => x.BranchId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
    //         entity.HasMany(x => x.Sales).WithOne(x => x.Branch).HasForeignKey(x => x.BranchId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);

    //     });
    //     //    modelBuilder.Entity<User>().HasMany(e => e.Products).WithOne(x => x.CreatedBy).HasForeignKey("UserId").IsRequired(true);



    //     //modelBuilder.Entity<AuditEntry>();

    //     //modelBuilder.Entity<Product>(e =>
    //     //{
    //     //    e.Property(b => b.Purchase_Rate).HasColumnType("decimal(10, 2)");
    //     //    e.Property(b => b.Purchase_Rate).HasColumnType("decimal(10, 2)");
    //     //});

    // }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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

        modelBuilder.Entity<Purchase>(entity =>
        {
            entity.HasIndex(x => x.TenantId);
            if (_tenantId != Guid.Empty)
                entity.HasQueryFilter(x => x.TenantId == _tenantId);
        });

        modelBuilder.Entity<PurchaseDetail>(entity =>
        {
            entity.HasIndex(x => x.TenantId);
            if (_tenantId != Guid.Empty)
                entity.HasQueryFilter(x => x.TenantId == _tenantId);
        });

        modelBuilder.Entity<Sales>(entity =>
        {
            entity.HasIndex(x => x.TenantId);
            if (_tenantId != Guid.Empty)
                entity.HasQueryFilter(x => x.TenantId == _tenantId);
        });

        modelBuilder.Entity<SalesDetail>(entity =>
        {
            entity.HasIndex(x => x.TenantId);
            if (_tenantId != Guid.Empty)
                entity.HasQueryFilter(x => x.TenantId == _tenantId);
        });

        modelBuilder.Entity<Stock>(entity =>
        {
            entity.HasIndex(x => x.TenantId);
            if (_tenantId != Guid.Empty)
                entity.HasQueryFilter(x => x.TenantId == _tenantId);
        });

        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasIndex(x => x.TenantId);
            entity.HasQueryFilter(x => x.TenantId == _tenantId);
        });

        modelBuilder.Entity<SaleReturn>(entity =>
        {
            entity.HasIndex(x => x.TenantId);
            if (_tenantId != Guid.Empty)
                entity.HasQueryFilter(x => x.TenantId == _tenantId);
        });

        modelBuilder.Entity<SaleReturnDetail>(entity =>
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

            entity.HasMany(x => x.Purchases)
                  .WithOne(x => x.Branch)
                  .HasForeignKey(x => x.BranchId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.Sales)
                  .WithOne(x => x.Branch)
                  .HasForeignKey(x => x.BranchId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.Restrict);
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

        return base.SaveChangesAsync(cancellationToken);
    }
}
