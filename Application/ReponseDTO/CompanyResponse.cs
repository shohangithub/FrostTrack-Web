namespace Application.ReponseDTO;

public record CompanyResponse(
    int Id,
    string Name,
    string LogoUrl,
    string BusinessCurrency,
    string CurrencySymbol,
    string Description,
    bool AutoInvoicePrint,
    bool AutoGenerateBookingNo,
    string InvoiceHeader,
    string InvoiceFooter,
    bool IsSingleBranch,
    int CodeGeneration,
    bool IsActive,
    string Status
);

public record CompanyListResponse(
    int Id,
    string Name,
    string BusinessCurrency,
    string CurrencySymbol,
    int CodeGeneration,
    string CodeGenerationName,
    bool AutoGenerateBookingNo,
    bool IsActive,
    string Status
);
