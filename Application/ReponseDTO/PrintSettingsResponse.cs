using Domain.Entitites;

namespace Application.ReponseDTO
{
    public class PrintSettingsResponse
    {
        public int Id { get; set; }
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;

        // Company Information
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyAddress { get; set; } = string.Empty;
        public string CompanyPhone { get; set; } = string.Empty;
        public string CompanyEmail { get; set; } = string.Empty;
        public string CompanyWebsite { get; set; } = string.Empty;
        public string LogoUrl { get; set; } = string.Empty;

        // Branch Information
        public string BranchAddress { get; set; } = string.Empty;
        public string BranchPhone { get; set; } = string.Empty;

        // Print Settings
        public bool ShowLogo { get; set; } = true;
        public bool ShowBranchInfo { get; set; } = true;
        public string PaperSize { get; set; } = "A4"; // A4, A5, RECEIPT, THERMAL
        public string Orientation { get; set; } = "portrait"; // portrait, landscape
        public string FontSize { get; set; } = "medium"; // small, medium, large
        public int DefaultCopies { get; set; } = 1;

        // Footer Information
        public string FooterText { get; set; } = "Thank you for your business!";
        public string TermsAndConditions { get; set; } = string.Empty;
        public string ThankYouMessage { get; set; } = "Payment received successfully!";
        public string AuthorizedBy { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;

        // Receipt specific settings
        public bool ShowPaymentDetails { get; set; } = true;
        public bool ShowSupplierInfo { get; set; } = true;
        public bool ShowAmountSummary { get; set; } = true;
        public bool ShowNotes { get; set; } = true;
        public string ReceiptNumberPrefix { get; set; } = "PAY-";
        public string PaymentReceiptTitle { get; set; } = "PAYMENT RECEIPT";
    }
}