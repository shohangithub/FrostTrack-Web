using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entitites
{
    [Table("PrintSettings")]
    public class PrintSettings : BaseEntity<int>
    {
        [Required]
        public int BranchId { get; set; }

        // Company Information
        [Required]
        [StringLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [StringLength(500)]
        public string CompanyAddress { get; set; } = string.Empty;

        [StringLength(50)]
        public string CompanyPhone { get; set; } = string.Empty;

        [StringLength(100)]
        public string CompanyEmail { get; set; } = string.Empty;

        [StringLength(200)]
        public string CompanyWebsite { get; set; } = string.Empty;

        [StringLength(500)]
        public string LogoUrl { get; set; } = string.Empty;

        // Branch Information
        [StringLength(500)]
        public string BranchAddress { get; set; } = string.Empty;

        [StringLength(50)]
        public string BranchPhone { get; set; } = string.Empty;

        // Print Settings
        public bool ShowLogo { get; set; } = true;
        public bool ShowBranchInfo { get; set; } = true;

        [StringLength(20)]
        public string PaperSize { get; set; } = "A4"; // A4, A5, RECEIPT, THERMAL

        [StringLength(20)]
        public string Orientation { get; set; } = "portrait"; // portrait, landscape

        [StringLength(20)]
        public string FontSize { get; set; } = "medium"; // small, medium, large

        public int DefaultCopies { get; set; } = 1;

        // Footer Information
        [StringLength(500)]
        public string FooterText { get; set; } = "Thank you for your business!";

        [StringLength(1000)]
        public string TermsAndConditions { get; set; } = string.Empty;

        [StringLength(200)]
        public string ThankYouMessage { get; set; } = "Payment received successfully!";

        [StringLength(100)]
        public string AuthorizedBy { get; set; } = string.Empty;

        [StringLength(200)]
        public string Signature { get; set; } = string.Empty;

        // Receipt specific settings
        public bool ShowPaymentDetails { get; set; } = true;
        public bool ShowSupplierInfo { get; set; } = true;
        public bool ShowAmountSummary { get; set; } = true;
        public bool ShowNotes { get; set; } = true;

        [StringLength(10)]
        public string ReceiptNumberPrefix { get; set; } = "PAY-";

        [StringLength(50)]
        public string PaymentReceiptTitle { get; set; } = "PAYMENT RECEIPT";

        // Navigation properties
        [ForeignKey("BranchId")]
        public virtual Branch? Branch { get; set; }
    }
}