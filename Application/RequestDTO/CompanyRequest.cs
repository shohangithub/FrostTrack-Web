namespace Application.RequestDTO;

public record CompanyRequest(
    string Name,
    string? LogoUrl,
    string? BusinessCurrency,
    string? CurrencySymbol,
    string? Description,
    bool AutoInvoicePrint,
    string? InvoiceHeader,
    string? InvoiceFooter,
    bool IsSingleBranch,
    int CodeGeneration,
    bool IsActive
);
