using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Identity;
using Domain;
using Persistence.Repositories;
using Persistence.SeedData;
using Application.Services.Common;
using Application.Contractors;
using System.Text;

namespace Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {

        services.AddContextRepository(configuration);

        // Configure ASP.NET Core Identity using the ApplicationDbContext
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 6;
            options.Password.RequireNonAlphanumeric = false;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();


        return services;
    }

    private static IServiceCollection AddContextRepository(this IServiceCollection services, IConfiguration configuration)
    {
        #region register db context provider
        //services.AddScoped<DbContext>();
        services.AddDbContext<ApplicationDbContext>(options =>
         options.UseSqlServer(configuration.GetConnectionString("ApplicationDbContext")));

        services.AddTransient<ApplicationDbContextInitializer>();
        #endregion

        #region register repositories
        services.AddScoped<IRepository<ApplicationUser, int>, Repository<ApplicationUser, int>>();
        services.AddScoped<IRepository<Branch, int>, Repository<Branch, int>>();
        services.AddScoped<IRepository<ProductCategory, int>, Repository<ProductCategory, int>>();
        services.AddScoped<IRepository<BaseUnit, int>, Repository<BaseUnit, int>>();
        services.AddScoped<IRepository<UnitConversion, int>, Repository<UnitConversion, int>>();
        services.AddScoped<IRepository<Customer, int>, Repository<Customer, int>>();
        services.AddScoped<IRepository<Supplier, int>, Repository<Supplier, int>>();
        services.AddScoped<IRepository<Company, int>, Repository<Company, int>>();
        services.AddScoped<IRepository<Product, int>, Repository<Product, int>>();
        services.AddScoped<IRepository<PaymentMethod, int>, Repository<PaymentMethod, int>>();
        services.AddScoped<IRepository<Purchase, long>, Repository<Purchase, long>>();
        services.AddScoped<IRepository<SupplierPayment, long>, Repository<SupplierPayment, long>>();
        services.AddScoped<IRepository<Sales, long>, Repository<Sales, long>>();
        services.AddScoped<IRepository<SaleReturn, long>, Repository<SaleReturn, long>>();
        services.AddScoped<IRepository<Damage, int>, Repository<Damage, int>>();
        services.AddScoped<IRepository<Asset, int>, Repository<Asset, int>>();
        services.AddScoped<IRepository<Employee, int>, Repository<Employee, int>>();
        services.AddScoped<IRepository<Booking, Guid>, Repository<Booking, Guid>>();
        services.AddScoped<IRepository<BookingDetail, Guid>, Repository<BookingDetail, Guid>>();
        services.AddScoped<IRepository<Delivery, Guid>, Repository<Delivery, Guid>>();
        services.AddScoped<IRepository<DeliveryDetail, Guid>, Repository<DeliveryDetail, Guid>>();

        services.AddScoped<IRepository<Bank, int>, Repository<Bank, int>>();
        services.AddScoped<IRepository<BankTransaction, long>, Repository<BankTransaction, long>>();

        services.AddScoped<IStockRepository, StockRepository>();
        services.AddScoped<ISalesRepository, SalesRepository>();
        services.AddScoped<ISaleReturnRepository, SaleReturnRepository>();
        services.AddScoped<IPurchaseRepository, PurchaseRepository>();
        services.AddScoped<ISupplierPaymentRepository, SupplierPaymentRepository>();
        services.AddScoped<IPaymentMethodRepository, PaymentMethodRepository>();
        services.AddScoped<IPrintSettingsRepository, PrintSettingsRepository>();
        services.AddScoped<IPurchaseReportRepository, PurchaseReportRepository>();
        services.AddScoped<IProductReceiveRepository, ProductReceiveRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();

        // Register DefaultValueInjector for repositories that need it
        services.AddScoped<DefaultValueInjector>();
        #endregion

        return services;
    }


}