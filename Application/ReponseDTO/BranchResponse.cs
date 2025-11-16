namespace Application.ReponseDTO;
public record BranchResponse(
    int Id,
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
    bool IsActive,
    string Status
    );
public record BranchListResponse(
    int Id,
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
    string Status
    );