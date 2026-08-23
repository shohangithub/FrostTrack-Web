using Application.Services;
using Application.Validators;
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
        services.AddMemoryCache();
        services.AddScoped<ICodeGenerationService, CodeGenerationService>();

        services.AddScoped<IUserTokenService, UserTokenService>();
        services.AddScoped<IUserService<int>, UserService>();
        services.AddScoped<IAssignClaimService, AssignClaimService>();
        services.AddScoped<ICompanyService, CompanyService>();
        services.AddScoped<IBranchService, BranchService>();
        services.AddScoped<IProductCategoryService, ProductCategoryService>();
        services.AddScoped<IBaseUnitService, BaseUnitService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IUnitConversionService, UnitConversionService>();
        services.AddScoped<ITransactionHeadService, TransactionHeadService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IBankService, BankService>();
        services.AddScoped<IBankTransactionService, BankTransactionService>();
        services.AddScoped<IPaymentMethodService, PaymentMethodService>();
        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IPrintService, PrintService>();
        services.AddScoped<IDeliveryService, DeliveryService>();
        services.AddScoped<IDeliveryChallanService, DeliveryChallanService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IRecurringChargeService, RecurringChargeService>();
        services.AddScoped<IRecurringChargeManagementService, RecurringChargeManagementService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IBillCollectionService, BillCollectionService>();
        services.AddScoped<IStockReportService, StockReportService>();
        services.AddScoped<IDailyStockBookService, DailyStockBookService>();
        services.AddScoped<IDatewiseBookingReportService, DatewiseBookingReportService>();
        services.AddScoped<IDatewiseDeliveryReportService, DatewiseDeliveryReportService>();
        services.AddScoped<ICashBookService, CashBookService>();
        services.AddScoped<ILedgerBookService, LedgerBookService>();
        services.AddScoped<IBankBookService, BankBookService>();
        services.AddScoped<IGeneralLedgerService, GeneralLedgerService>();
        services.AddScoped<ISalaryPaymentService, SalaryPaymentService>();
        services.AddScoped<SalaryPaymentValidator>();
        services.AddScoped<ITrialBalanceService, TrialBalanceService>();
        services.AddScoped<IBalanceSheetService, BalanceSheetService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IEmployeeReportService, EmployeeReportService>();
        services.AddScoped<Application.Services.Common.IBalanceCalculatorService, Application.Services.Common.BalanceCalculatorService>();

        services.AddTransient<DefaultValueInjector>();

        return services;
    }

}
