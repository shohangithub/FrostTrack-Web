namespace Application.RequestDTO;

public record BranchRequest(
    string Name,
    string BranchCode,
    string? BusinessCurrency,
    string? CurrencySymbol,
    string Phone,
    string Address,
    bool AutoInvoicePrint,
    string? InvoiceHeader,
    string? InvoiceFooter,
    bool IsMainBranch,
    bool IsActive
    );