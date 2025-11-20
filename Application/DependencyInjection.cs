using Application.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {

        services.AddServices();

        return services;
    }


    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        services.AddScoped<IUserTokenService, UserTokenService>();
        services.AddScoped<IUserService<int>, UserService>();
        services.AddScoped<IAssignClaimService, AssignClaimService>();
        services.AddScoped<IBranchService, BranchService>();
        services.AddScoped<IProductCategoryService, ProductCategoryService>();
        services.AddScoped<IBaseUnitService, BaseUnitService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IUnitConversionService, UnitConversionService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IBankService, BankService>();
        services.AddScoped<IBankTransactionService, BankTransactionService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<IPurchaseService, PurchaseService>();
        services.AddScoped<ISupplierPaymentService, SupplierPaymentService>();
        services.AddScoped<IPaymentMethodService, PaymentMethodService>();
        services.AddScoped<ISalesService, SalesService>();
        services.AddScoped<ISaleReturnService, SaleReturnService>();
        services.AddScoped<IDamageService, DamageService>();
        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IPrintService, PrintService>();
        services.AddScoped<IPurchaseReportService, PurchaseReportService>();
        services.AddScoped<IProductReceiveService, ProductReceiveService>();
        services.AddScoped<IDeliveryService, DeliveryService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<ITransactionService, TransactionService>();

        services.AddTransient<DefaultValueInjector>();

        return services;
    }

}
