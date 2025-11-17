using Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace Persistence.SeedData;

public static class InitialiserExtensions
{
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitializer>();

        await initialiser.InitializeAsync();

        await initialiser.SeedAsync();
    }
}

public class ApplicationDbContextInitializer
{
    private readonly ILogger<ApplicationDbContextInitializer> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public ApplicationDbContextInitializer(ILogger<ApplicationDbContextInitializer> logger, ApplicationDbContext context, IConfiguration configuration)
    {
        _logger = logger;
        _context = context;
        _configuration = configuration;
    }

    public async Task InitializeAsync()
    {
        try
        {
            await _context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initialising the database.");
            throw;
        }
    }

    public async Task SeedAsync()
    {
        try
        {
            await TrySeedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    public async Task TrySeedAsync()
    {
        // Default data
        // Seed, if necessary

        var defaultTenantId = _configuration.GetSection("DefaultTenant:TenantId").Get<string>();
        var user = _configuration.GetSection("DefaultTenant:User").Get<string>() ?? "ris.shohan@gmail.com";
        var password = _configuration.GetSection("DefaultTenant:Password").Get<string>() ?? "1qazZAQ!";
        var tenantId = new Guid(defaultTenantId ?? Guid.NewGuid().ToString());

        #region SET COMPANY
        if (!_context.Companies.Any())
        {
            _context.Companies.Add(new Company
            {
                Name = "Sylhet Cold storage and ice company Ltd.",
                AutoInvoicePrint = true,
                BusinessCurrency = "BDT",
                CodeGeneration = ECodeGeneration.Company,
                CurrencySymbol = "৳",
                IsSingleBranch = true,
                IsActive = true,
                TenantId = tenantId
            });

            await _context.SaveChangesAsync();
        }
        #endregion

        #region SET BRANCHES
        if (!_context.Branches.Any())
        {
            var branches = new List<Branch>
    {
        new Branch
        {
            IsMainBranch = true,
            Name = "Main Branch",
            BranchCode = "B-00001",
            IsActive = true,
            TenantId = tenantId
        },
        //new Branch
        //{
        //    IsMainBranch = false,
        //    Name = "East Branch",
        //    BranchCode = "B-00002",
        //    IsActive = true,
        //    TenantId = tenantId
        //},
        //new Branch
        //{
        //    IsMainBranch = false,
        //    Name = "West Branch",
        //    BranchCode = "B-00003",
        //    IsActive = true,
        //    TenantId = tenantId
        //},
        //new Branch
        //{
        //    IsMainBranch = false,
        //    Name = "North Branch",
        //    BranchCode = "B-00004",
        //    IsActive = true,
        //    TenantId = tenantId
        //},
        //new Branch
        //{
        //    IsMainBranch = false,
        //    Name = "South Branch",
        //    BranchCode = "B-00005",
        //    IsActive = true,
        //    TenantId = tenantId
        //}
    };

            _context.Branches.AddRange(branches);
            await _context.SaveChangesAsync();
        }
        #endregion


        #region SET ROLES

        if (!_context.Roles.Any())
        {
            var roles = new List<ApplicationRole>
    {
        new ApplicationRole
        {
            Name = RoleNames.SuperAdmin,
            NormalizedName = RoleNames.SuperAdmin.ToUpper(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
        },
        new ApplicationRole
        {
            Name = RoleNames.Admin,
            NormalizedName = RoleNames.Admin.ToUpper(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
        },
        new ApplicationRole
        {
            Name = RoleNames.Manager,
            NormalizedName = RoleNames.Manager.ToUpper(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
        },
        new ApplicationRole
        {
            Name = RoleNames.Seller,
            NormalizedName = RoleNames.Seller.ToUpper(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
        },
        new ApplicationRole
        {
            Name = RoleNames.Standard,
            NormalizedName = RoleNames.Standard.ToUpper(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
        }
    };

            _context.Roles.AddRange(roles);
            await _context.SaveChangesAsync();
        }
        #endregion


        #region SET USERS

        var branchId = _context.Branches.Select(x => x.Id).FirstOrDefault();
        if (!_context.Users.Any())
        {
            var users = new List<ApplicationUser>
    {
        new ApplicationUser
        {
            UserName = user,
            NormalizedUserName = user.ToUpper(),
            Email = user,
            NormalizedEmail = user.ToUpper(),
            PasswordHash = "AQAAAAEAACcQAAAAELUblJDKkKsPIPOMNqNyztYnKdfBsD36SP4y3PdlSW/7wbLwZp6kLgPYCSzucgxN2A==", // Hash for "1qazZAQ!"
            IsActive = true,
            TenantId = tenantId,
            BranchId = branchId
        },
        new ApplicationUser
        {
            UserName = "john.doe@company.com",
            NormalizedUserName = "JOHN.DOE@COMPANY.COM",
            Email = "john.doe@company.com",
            NormalizedEmail = "JOHN.DOE@COMPANY.COM",
            PasswordHash = "AQAAAAEAACcQAAAAELUblJDKkKsPIPOMNqNyztYnKdfBsD36SP4y3PdlSW/7wbLwZp6kLgPYCSzucgxN2A==", // Hash for "1qazZAQ!"
            IsActive = true,
            TenantId = tenantId,
            BranchId = branchId
        },
        new ApplicationUser
        {
            UserName = "jane.smith@company.com",
            NormalizedUserName = "JANE.SMITH@COMPANY.COM",
            Email = "jane.smith@company.com",
            NormalizedEmail = "JANE.SMITH@COMPANY.COM",
            PasswordHash = "AQAAAAEAACcQAAAAELUblJDKkKsPIPOMNqNyztYnKdfBsD36SP4y3PdlSW/7wbLwZp6kLgPYCSzucgxN2A==", // Hash for "1qazZAQ!"
            IsActive = true,
            TenantId = tenantId,
            BranchId = branchId
        },
        new ApplicationUser
        {
            UserName = "mark.jones@company.com",
            NormalizedUserName = "MARK.JONES@COMPANY.COM",
            Email = "mark.jones@company.com",
            NormalizedEmail = "MARK.JONES@COMPANY.COM",
            PasswordHash = "AQAAAAEAACcQAAAAELUblJDKkKsPIPOMNqNyztYnKdfBsD36SP4y3PdlSW/7wbLwZp6kLgPYCSzucgxN2A==", // Hash for "1qazZAQ!"
            IsActive = true,
            TenantId = tenantId,
            BranchId = branchId
        },
        new ApplicationUser
        {
            UserName = "lisa.brown@company.com",
            NormalizedUserName = "LISA.BROWN@COMPANY.COM",
            Email = "lisa.brown@company.com",
            NormalizedEmail = "LISA.BROWN@COMPANY.COM",
            PasswordHash = "AQAAAAEAACcQAAAAELUblJDKkKsPIPOMNqNyztYnKdfBsD36SP4y3PdlSW/7wbLwZp6kLgPYCSzucgxN2A==", // Hash for "1qazZAQ!"
            IsActive = true,
            TenantId = tenantId,
            BranchId = branchId
        }
    };

            _context.Users.AddRange(users);
            await _context.SaveChangesAsync();
        }
        #endregion


        #region SET ASSIGNMENT ROLES

        if (!_context.UserRoles.Any())
        {
            var userRoles = new List<IdentityUserRole<int>>
    {
        new IdentityUserRole<int>
        {
            UserId = _context.Users.Where(u => u.UserName == user).Select(u => u.Id).FirstOrDefault(),
            RoleId = _context.Roles.Where(r => r.Name == RoleNames.SuperAdmin).Select(r => r.Id).FirstOrDefault()
        },
        new IdentityUserRole<int>
        {
            UserId = _context.Users.Where(u => u.UserName == "john.doe@company.com").Select(u => u.Id).FirstOrDefault(),
            RoleId = _context.Roles.Where(r => r.Name == RoleNames.Admin).Select(r => r.Id).FirstOrDefault()
        },
        new IdentityUserRole<int>
        {
            UserId = _context.Users.Where(u => u.UserName == "jane.smith@company.com").Select(u => u.Id).FirstOrDefault(),
            RoleId = _context.Roles.Where(r => r.Name == RoleNames.Manager).Select(r => r.Id).FirstOrDefault()
        },
        new IdentityUserRole<int>
        {
            UserId = _context.Users.Where(u => u.UserName == "mark.jones@company.com").Select(u => u.Id).FirstOrDefault(),
            RoleId = _context.Roles.Where(r => r.Name == RoleNames.Seller).Select(r => r.Id).FirstOrDefault()
        },
        new IdentityUserRole<int>
        {
            UserId = _context.Users.Where(u => u.UserName == "lisa.brown@company.com").Select(u => u.Id).FirstOrDefault(),
            RoleId = _context.Roles.Where(r => r.Name == RoleNames.Standard).Select(r => r.Id).FirstOrDefault()
        }
    };

            _context.UserRoles.AddRange(userRoles);
            await _context.SaveChangesAsync();
        }
        #endregion


        #region SET PRODUCT CATEGORIES
        var userId = _context.Users.Select(x => x.Id).FirstOrDefault();

        if (!_context.ProductCategories.Any())
        {
            var categories = new List<ProductCategory>
    {
        new ProductCategory
        {
            CategoryName = "General",
            CreatedById = userId,
            IsActive = true,
            Description = "It's a general category",
            TenantId = tenantId
        },
        // new ProductCategory
        // {
        //     CategoryName = "Electronics",
        //     CreatedById = userId,
        //     IsActive = true,
        //     Description = "Electronic items like phones, laptops, etc.",
        //     TenantId = tenantId
        // },
        // new ProductCategory
        // {
        //     CategoryName = "Furniture",
        //     CreatedById = userId,
        //     IsActive = true,
        //     Description = "Office and home furniture",
        //     TenantId = tenantId
        // },
        // new ProductCategory
        // {
        //     CategoryName = "Groceries",
        //     CreatedById = userId,
        //     IsActive = true,
        //     Description = "Daily consumable goods and food items",
        //     TenantId = tenantId
        // },
        // new ProductCategory
        // {
        //     CategoryName = "Stationery",
        //     CreatedById = userId,
        //     IsActive = true,
        //     Description = "Office and school supplies",
        //     TenantId = tenantId
        // }
    };

            _context.ProductCategories.AddRange(categories);
            await _context.SaveChangesAsync();
        }
        #endregion


        #region SET DEFAULT CUSTOMERS
        if (!_context.Customers.Any())
        {
            var customers = new List<Customer>
    {
        new Customer
        {
            CustomerName = "General",
            BranchId = branchId,
            CustomerCode = string.Empty,
            CustomerType = ECustomerType.Retail,
            CreatedById = userId,
            IsActive = true,
            IsSystemDefault = true,
            TenantId = tenantId
        },
        new Customer
        {
            CustomerName = "John Doe",
            BranchId = branchId,
            CustomerCode = "C-00001",
            CustomerType = ECustomerType.Retail,
            CreatedById = userId,
            IsActive = true,
            IsSystemDefault = false,
            TenantId = tenantId
        },
        new Customer
        {
            CustomerName = "Acme Corporation",
            BranchId = branchId,
            CustomerCode = "C-00002",
            CustomerType = ECustomerType.Wholesale,
            CreatedById = userId,
            IsActive = true,
            IsSystemDefault = false,
            TenantId = tenantId
        },
        new Customer
        {
            CustomerName = "Jane Smith",
            BranchId = branchId,
            CustomerCode = "C-00003",
            CustomerType = ECustomerType.Retail,
            CreatedById = userId,
            IsActive = true,
            IsSystemDefault = false,
            TenantId = tenantId
        },
        new Customer
        {
            CustomerName = "Mega Builders Ltd.",
            BranchId = branchId,
            CustomerCode = "C-00004",
            CustomerType = ECustomerType.Wholesale,
            CreatedById = userId,
            IsActive = true,
            IsSystemDefault = false,
            TenantId = tenantId
        }
    };

            _context.Customers.AddRange(customers);
            await _context.SaveChangesAsync();
        }
        #endregion


        #region SET DEFAULT SUPPLIERS
        if (!_context.Suppliers.Any())
        {
            var suppliers = new List<Supplier>
    {
        new Supplier
        {
            SupplierName = "General",
            BranchId = branchId,
            SupplierCode = string.Empty,
            CreatedById = userId,
            IsActive = true,
            IsSystemDefault = true,
            TenantId = tenantId
        },
        // new Supplier
        // {
        //     SupplierName = "Office Supplies Co.",
        //     BranchId = branchId,
        //     SupplierCode = "S-00001",
        //     CreatedById = userId,
        //     IsActive = true,
        //     IsSystemDefault = false,
        //     TenantId = tenantId
        // },
        // new Supplier
        // {
        //     SupplierName = "Tech Hardware Ltd.",
        //     BranchId = branchId,
        //     SupplierCode = "S-00002",
        //     CreatedById = userId,
        //     IsActive = true,
        //     IsSystemDefault = false,
        //     TenantId = tenantId
        // },
        // new Supplier
        // {
        //     SupplierName = "Clean & Go Services",
        //     BranchId = branchId,
        //     SupplierCode = "S-00003",
        //     CreatedById = userId,
        //     IsActive = true,
        //     IsSystemDefault = false,
        //     TenantId = tenantId
        // },
        // new Supplier
        // {
        //     SupplierName = "Fresh Foods Distributor",
        //     BranchId = branchId,
        //     SupplierCode = "S-00004",
        //     CreatedById = userId,
        //     IsActive = true,
        //     IsSystemDefault = false,
        //     TenantId = tenantId
        // }
    };

            _context.Suppliers.AddRange(suppliers);
            await _context.SaveChangesAsync();
        }
        #endregion


        #region SET BASE UNITS
        if (!_context.ProductUnits.Any())
        {
            var baseUnits = new List<BaseUnit>
            {
                new BaseUnit
                {
                    UnitName = "Piece",
                    Description = "Individual pieces or items",
                    CreatedById = userId,
                    IsActive = true,
                    TenantId = tenantId
                },
                new BaseUnit
                {
                    UnitName = "Kilogram",
                    Description = "Weight measurement in kilograms",
                    CreatedById = userId,
                    IsActive = true,
                    TenantId = tenantId
                },
                // new BaseUnit
                // {
                //     UnitName = "Gram",
                //     Description = "Weight measurement in grams",
                //     CreatedById = userId,
                //     IsActive = true,
                //     TenantId = tenantId
                // },
                // new BaseUnit
                // {
                //     UnitName = "Liter",
                //     Description = "Volume measurement in liters",
                //     CreatedById = userId,
                //     IsActive = true,
                //     TenantId = tenantId
                // },
                // new BaseUnit
                // {
                //     UnitName = "Meter",
                //     Description = "Length measurement in meters",
                //     CreatedById = userId,
                //     IsActive = true,
                //     TenantId = tenantId
                // }
            };

            _context.ProductUnits.AddRange(baseUnits);
            await _context.SaveChangesAsync();
        }
        #endregion

        #region SET UNIT CONVERSIONS
        var baseUnitPiece = _context.ProductUnits.Where(x => x.UnitName == "Kilogram").FirstOrDefault();

        if (!_context.UnitConversions.Any() && baseUnitPiece != null)
        {
            var unitConversions = new List<UnitConversion>
            {
                // new UnitConversion
                // {
                //     UnitName = "pcs",
                //     BaseUnitId = baseUnitPiece.Id,
                //     BaseUnit = baseUnitPiece,
                //     ConversionValue = 1.0f,
                //     Description = "Single piece",
                //     CreatedById = userId,
                //     IsActive = true,
                //     TenantId = tenantId
                // },
                new UnitConversion
                {
                    UnitName = "kg",
                    BaseUnitId = baseUnitPiece.Id,
                    BaseUnit = baseUnitPiece,
                    ConversionValue = 12.0f,
                    Description = "1 kilogram",
                    CreatedById = userId,
                    IsActive = true,
                    TenantId = tenantId
                },
                new UnitConversion
                {
                    UnitName = "Caret",
                    BaseUnitId = baseUnitPiece.Id,
                    BaseUnit = baseUnitPiece,
                    ConversionValue = 10.0f,
                    Description = "10 Kgs",
                    CreatedById = userId,
                    IsActive = true,
                    TenantId = tenantId
                },
                // new UnitConversion
                // {
                //     UnitName = "Box",
                //     BaseUnitId = baseUnitPiece.Id,
                //     BaseUnit = baseUnitPiece,
                //     ConversionValue = 10.0f,
                //     Description = "10 pieces",
                //     CreatedById = userId,
                //     IsActive = true,
                //     TenantId = tenantId
                // },
                // new UnitConversion
                // {
                //     UnitName = "Pack",
                //     BaseUnitId = baseUnitPiece.Id,
                //     BaseUnit = baseUnitPiece,
                //     ConversionValue = 6.0f,
                //     Description = "6 pieces",
                //     CreatedById = userId,
                //     IsActive = true,
                //     TenantId = tenantId
                // }
            };

            _context.UnitConversions.AddRange(unitConversions);
            await _context.SaveChangesAsync();
        }
        #endregion

        #region SET PRODUCTS
        var categoryId = _context.ProductCategories.Where(x => x.CategoryName == "General").Select(x => x.Id).FirstOrDefault();
        var unitConversionId = _context.UnitConversions.Where(x => x.UnitName == "kg").Select(x => x.Id).FirstOrDefault();

        if (!_context.Products.Any())
        {
            var products = new List<Product>
            {

// খেজুর (ডেগলেট নূর)
// খেজুর (মাজাফাতি)
// খেজুর (খুদরি)
// খেজুর (সায়ার)

                new Product
                {
                    ProductName = "খেজুর (আজওয়া)",
                    ProductCode = "P-00001",
                    CategoryId = categoryId,
                    BranchId = branchId,
                    DefaultUnitId = unitConversionId,
                    PurchaseRate = 50.00m,
                    SellingRate = 75.00m,
                    IsRawMaterial = false,
                    IsFinishedGoods = true,
                    IsProductAsService = false,
                    CreatedById = userId,
                    IsActive = true,
                    TenantId = tenantId
                },
                new Product
                {
                    ProductName = "খেজুর (মেডজুল)",
                    ProductCode = "P-00002",
                    CategoryId = categoryId,
                    BranchId = branchId,
                    DefaultUnitId = unitConversionId,
                    PurchaseRate = 100.00m,
                    SellingRate = 150.00m,
                    IsRawMaterial = false,
                    IsFinishedGoods = true,
                    IsProductAsService = false,
                    CreatedById = userId,
                    IsActive = true,
                    TenantId = tenantId
                },
                new Product
                {
                    ProductName = "খেজুর (মাবরুম)",
                    ProductCode = "P-00003",
                    CategoryId = categoryId,
                    BranchId = branchId,
                    DefaultUnitId = unitConversionId,
                    PurchaseRate = 25.00m,
                    SellingRate = 40.00m,
                    IsRawMaterial = false,
                    IsFinishedGoods = true,
                    IsProductAsService = false,
                    CreatedById = userId,
                    IsActive = true,
                    TenantId = tenantId
                },
                new Product
                {
                    ProductName = "খেজুর (সুক্কারি)",
                    ProductCode = "P-00004",
                    CategoryId = categoryId,
                    BranchId = branchId,
                    DefaultUnitId = unitConversionId,
                    PurchaseRate = 200.00m,
                    SellingRate = 300.00m,
                    IsRawMaterial = false,
                    IsFinishedGoods = true,
                    IsProductAsService = false,
                    CreatedById = userId,
                    IsActive = true,
                    TenantId = tenantId
                },
                new Product
                {
                    ProductName = "খেজুর (সাফাওয়ি)",
                    ProductCode = "P-00005",
                    CategoryId = categoryId,
                    BranchId = branchId,
                    DefaultUnitId = unitConversionId,
                    PurchaseRate = 80.00m,
                    SellingRate = 120.00m,
                    IsRawMaterial = false,
                    IsFinishedGoods = true,
                    IsProductAsService = false,
                    CreatedById = userId,
                    IsActive = true,
                    TenantId = tenantId
                },
                new Product
                {
                    ProductName = "খেজুর (দাব্বাস)",
                    ProductCode = "P-00006",
                    CategoryId = categoryId,
                    BranchId = branchId,
                    DefaultUnitId = unitConversionId,
                    PurchaseRate = 60.00m,
                    SellingRate = 90.00m,
                    IsRawMaterial = false,
                    IsFinishedGoods = true,
                    IsProductAsService = false,
                    CreatedById = userId,
                    IsActive = true,
                    TenantId = tenantId
                },
                new Product
                {
                    ProductName = "খেজুর (বরই / পালম)",
                    ProductCode = "P-00007",
                    CategoryId = categoryId,
                    BranchId = branchId,
                    DefaultUnitId = unitConversionId,
                    PurchaseRate = 70.00m,
                    SellingRate = 100.00m,
                    IsRawMaterial = false,
                    IsFinishedGoods = true,
                    IsProductAsService = false,
                    CreatedById = userId,
                    IsActive = true,
                    TenantId = tenantId
                },
                new Product
                {
                    ProductName = "খেজুর (জাহেদি)",
                    ProductCode = "P-00008",
                    CategoryId = categoryId,
                    BranchId = branchId,
                    DefaultUnitId = unitConversionId,
                    PurchaseRate = 90.00m,
                    SellingRate = 130.00m,
                    IsRawMaterial = false,
                    IsFinishedGoods = true,
                    IsProductAsService = false,
                    CreatedById = userId,
                    IsActive = true,
                    TenantId = tenantId
                },
                new Product
                {
                    ProductName = "খেজুর (কালমি)",
                    ProductCode = "P-00009",
                    CategoryId = categoryId,
                    BranchId = branchId,
                    DefaultUnitId = unitConversionId,
                    PurchaseRate = 110.00m,
                    SellingRate = 160.00m,
                    IsRawMaterial = false,
                    IsFinishedGoods = true,
                    IsProductAsService = false,
                    CreatedById = userId,
                    IsActive = true,
                    TenantId = tenantId
                },
                new Product
                {
                    ProductName = "খেজুর (পিয়ারম)",
                    ProductCode = "P-00010",
                    CategoryId = categoryId,
                    BranchId = branchId,
                    DefaultUnitId = unitConversionId,
                    PurchaseRate = 95.00m,
                    SellingRate = 140.00m,
                    IsRawMaterial = false,
                    IsFinishedGoods = true,
                    IsProductAsService = false,
                    CreatedById = userId,
                    IsActive = true,
                    TenantId = tenantId
                },

            };

            _context.Products.AddRange(products);
            await _context.SaveChangesAsync();
        }
        #endregion

        #region SET SAMPLE PURCHASES
        var supplierId = _context.Suppliers.Where(x => x.SupplierName == "General").Select(x => x.Id).FirstOrDefault();
        var productIds = _context.Products.Take(3).Select(x => x.Id).ToList();

        if (!_context.Purchases.Any() && productIds.Count >= 3)
        {
            var purchases = new List<Purchase>
            {
                new Purchase
                {
                    InvoiceNumber = "PUR-001",
                    InvoiceDate = DateTime.UtcNow.AddDays(-30),
                    SupplierId = supplierId,
                    BranchId = branchId,
                    Subtotal = 500.00m,
                    VatPercent = 0.0f,
                    VatAmount = 0.00m,
                    DiscountPercent = 0.0f,
                    DiscountAmount = 0.00m,
                    OtherCost = 0.00m,
                    InvoiceAmount = 500.00m,
                    PaidAmount = 500.00m,
                    CreatedById = userId,
                    TenantId = tenantId,
                    PurchaseDetails = new List<PurchaseDetail>
                    {
                        new PurchaseDetail
                        {
                            ProductId = productIds[0],
                            PurchaseUnitId = unitConversionId,
                            PurchaseRate = 50.00m,
                            PurchaseQuantity = 10.0f,
                            PurchaseAmount = 500.00m,
                            CreatedById = userId,
                            TenantId = tenantId
                        }
                    }
                },
                new Purchase
                {
                    InvoiceNumber = "PUR-002",
                    InvoiceDate = DateTime.UtcNow.AddDays(-25),
                    SupplierId = supplierId,
                    BranchId = branchId,
                    Subtotal = 750.00m,
                    VatPercent = 0.0f,
                    VatAmount = 0.00m,
                    DiscountPercent = 0.0f,
                    DiscountAmount = 0.00m,
                    OtherCost = 0.00m,
                    InvoiceAmount = 750.00m,
                    PaidAmount = 750.00m,
                    CreatedById = userId,
                    TenantId = tenantId,
                    PurchaseDetails = new List<PurchaseDetail>
                    {
                        new PurchaseDetail
                        {
                            ProductId = productIds[1],
                            PurchaseUnitId = unitConversionId,
                            PurchaseRate = 100.00m,
                            PurchaseQuantity = 5.0f,
                            PurchaseAmount = 500.00m,
                            CreatedById = userId,
                            TenantId = tenantId
                        },
                        new PurchaseDetail
                        {
                            ProductId = productIds[2],
                            PurchaseUnitId = unitConversionId,
                            PurchaseRate = 25.00m,
                            PurchaseQuantity = 10.0f,
                            PurchaseAmount = 250.00m,
                            CreatedById = userId,
                            TenantId = tenantId
                        }
                    }
                }
            };

            _context.Purchases.AddRange(purchases);
            await _context.SaveChangesAsync();
        }
        #endregion

        #region SET STOCKS

        if (!_context.Stocks.Any())
        {
            var _unitConversionId = _context.UnitConversions.Select(u => u.Id).FirstOrDefault();
            var _branchId = _context.Branches.Select(b => b.Id).FirstOrDefault();
            var _userId = _context.Users.Select(u => u.Id).FirstOrDefault();

            var stocks = new List<Stock>
    {
        new Stock
        {
            ProductId = _context.Products.Select(p => p.Id).ToArray()[0],
            UnitConversionId = unitConversionId,
            StockQuantity = 100,
            LastPurchaseRate = 50.00m,
            BranchId = branchId,
            CreatedById = userId,
            TenantId = tenantId
        },
        new Stock
        {
            ProductId = _context.Products.Select(p => p.Id).ToArray()[1],
            UnitConversionId = _unitConversionId,
            StockQuantity = 250,
            LastPurchaseRate = 20.50m,
            BranchId = _branchId,
            CreatedById = _userId,
            TenantId = tenantId,
        },
        new Stock
        {
            ProductId = _context.Products.Select(p => p.Id).ToArray()[2],
            UnitConversionId = _unitConversionId,
            StockQuantity = 75,
            LastPurchaseRate = 15.75m,
            BranchId = _branchId,
            CreatedById = _userId,
            TenantId = tenantId,
        },
        new Stock
        {
            ProductId = _context.Products.Select(p => p.Id).ToArray()[3],
            UnitConversionId = _unitConversionId,
            StockQuantity = 500,
            LastPurchaseRate = 5.25m,
            BranchId = _branchId,
            CreatedById = _userId,
            TenantId = tenantId
        },
        new Stock
        {
            ProductId = _context.Products.Select(p => p.Id).ToArray()[4],
            UnitConversionId = _unitConversionId,
            StockQuantity = 300,
            LastPurchaseRate = 32.00m,
            BranchId = _branchId,
            CreatedById = _userId,
            TenantId = tenantId
        }
    };

            _context.Stocks.AddRange(stocks);
            await _context.SaveChangesAsync();
        }
        #endregion


        // #region SET SAMPLE SALES
        // var customerId = _context.Customers.Where(x => x.CustomerName == "General").Select(x => x.Id).FirstOrDefault();

        // if (!_context.Sales.Any() && productIds.Count >= 3)
        // {
        //     var sales = new List<Sales>
        //     {
        //         new Sales
        //         {
        //             InvoiceNumber = "INV-001",
        //             InvoiceDate = DateTime.UtcNow.AddDays(-15),
        //             SalesType = "Cash",
        //             CustomerId = customerId,
        //             BranchId = branchId,
        //             Subtotal = 600.00m,
        //             VatPercent = 0.0f,
        //             VatAmount = 0.00m,
        //             DiscountPercent = 0.0f,
        //             DiscountAmount = 0.00m,
        //             OtherCost = 0.00m,
        //             InvoiceAmount = 600.00m,
        //             PaidAmount = 600.00m,
        //             CreatedById = userId,
        //             TenantId = tenantId,
        //             SalesDetails = new List<SalesDetail>
        //             {
        //                 new SalesDetail
        //                 {
        //                     ProductId = productIds[0],
        //                     SalesUnitId = unitConversionId,
        //                     SalesRate = 75.00m,
        //                     SalesQuantity = 5.0f,
        //                     SalesAmount = 375.00m,
        //                     CreatedById = userId,
        //                     TenantId = tenantId
        //                 },
        //                 new SalesDetail
        //                 {
        //                     ProductId = productIds[1],
        //                     SalesUnitId = unitConversionId,
        //                     SalesRate = 150.00m,
        //                     SalesQuantity = 1.5f,
        //                     SalesAmount = 225.00m,
        //                     CreatedById = userId,
        //                     TenantId = tenantId
        //                 }
        //             }
        //         },
        //         new Sales
        //         {
        //             InvoiceNumber = "INV-002",
        //             InvoiceDate = DateTime.UtcNow.AddDays(-10),
        //             SalesType = "Cash",
        //             CustomerId = customerId,
        //             BranchId = branchId,
        //             Subtotal = 400.00m,
        //             VatPercent = 0.0f,
        //             VatAmount = 0.00m,
        //             DiscountPercent = 5.0f,
        //             DiscountAmount = 20.00m,
        //             OtherCost = 0.00m,
        //             InvoiceAmount = 380.00m,
        //             PaidAmount = 380.00m,
        //             CreatedById = userId,
        //             TenantId = tenantId,
        //             SalesDetails = new List<SalesDetail>
        //             {
        //                 new SalesDetail
        //                 {
        //                     ProductId = productIds[2],
        //                     SalesUnitId = unitConversionId,
        //                     SalesRate = 40.00m,
        //                     SalesQuantity = 10.0f,
        //                     SalesAmount = 400.00m,
        //                     CreatedById = userId,
        //                     TenantId = tenantId
        //                 }
        //             }
        //         }
        //     };

        //     _context.Sales.AddRange(sales);
        //     await _context.SaveChangesAsync();
        // }
        // #endregion

        // #region SET PAYMENT METHODS
        // if (!_context.PaymentMethods.Any())
        // {
        //     var paymentMethods = new List<PaymentMethod>
        //     {
        //         new PaymentMethod
        //         {
        //             MethodName = "Cash",
        //             Code = "CASH-001",
        //             Description = "Physical cash payment",
        //             Category = "Cash",
        //             RequiresBankAccount = false,
        //             RequiresCheckDetails = false,
        //             RequiresOnlineDetails = false,
        //             RequiresMobileWalletDetails = false,
        //             RequiresCardDetails = false,
        //             IsActive = true,
        //             SortOrder = 1,
        //             IconClass = "fa fa-money-bill-wave",
        //             BranchId = branchId,
        //             CreatedById = userId,
        //             TenantId = tenantId
        //         },
        //         new PaymentMethod
        //         {
        //             MethodName = "Bank Transfer",
        //             Code = "BANK-001",
        //             Description = "Bank account transfer",
        //             Category = "Bank",
        //             RequiresBankAccount = true,
        //             RequiresCheckDetails = false,
        //             RequiresOnlineDetails = false,
        //             RequiresMobileWalletDetails = false,
        //             RequiresCardDetails = false,
        //             IsActive = true,
        //             SortOrder = 2,
        //             IconClass = "fa fa-university",
        //             BranchId = branchId,
        //             CreatedById = userId,
        //             TenantId = tenantId
        //         },
        //         new PaymentMethod
        //         {
        //             MethodName = "Credit Card",
        //             Code = "CARD-001",
        //             Description = "Credit/Debit card payment",
        //             Category = "Card",
        //             RequiresBankAccount = false,
        //             RequiresCheckDetails = false,
        //             RequiresOnlineDetails = false,
        //             RequiresMobileWalletDetails = false,
        //             RequiresCardDetails = true,
        //             IsActive = true,
        //             SortOrder = 3,
        //             IconClass = "fa fa-credit-card",
        //             BranchId = branchId,
        //             CreatedById = userId,
        //             TenantId = tenantId
        //         },
        //         new PaymentMethod
        //         {
        //             MethodName = "Mobile Wallet",
        //             Code = "MOBILE-001",
        //             Description = "Mobile wallet payment",
        //             Category = "Digital",
        //             RequiresBankAccount = false,
        //             RequiresCheckDetails = false,
        //             RequiresOnlineDetails = false,
        //             RequiresMobileWalletDetails = true,
        //             RequiresCardDetails = false,
        //             IsActive = true,
        //             SortOrder = 4,
        //             IconClass = "fa fa-mobile-alt",
        //             BranchId = branchId,
        //             CreatedById = userId,
        //             TenantId = tenantId
        //         },
        //         new PaymentMethod
        //         {
        //             MethodName = "Check",
        //             Code = "CHECK-001",
        //             Description = "Check payment",
        //             Category = "Bank",
        //             RequiresBankAccount = true,
        //             RequiresCheckDetails = true,
        //             RequiresOnlineDetails = false,
        //             RequiresMobileWalletDetails = false,
        //             RequiresCardDetails = false,
        //             IsActive = true,
        //             SortOrder = 5,
        //             IconClass = "fa fa-money-check",
        //             BranchId = branchId,
        //             CreatedById = userId,
        //             TenantId = tenantId
        //         }
        //     };

        //     _context.PaymentMethods.AddRange(paymentMethods);
        //     await _context.SaveChangesAsync();
        // }
        // #endregion
    }
}
