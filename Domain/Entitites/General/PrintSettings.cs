namespace Domain.Entitites;

[Table("PrintSettings")]
public class PrintSettings : BaseEntity<int>
{
    public int BranchId { get; set; }
    public Branch? Branch { get; set; }

    [MaxLength(200)]
    public string CompanyName { get; set; } = string.Empty;
    [MaxLength(500)]
    public string CompanyAddress { get; set; } = string.Empty;
    [MaxLength(50)]
    public string CompanyPhone { get; set; } = string.Empty;
    [MaxLength(100)]
    public string CompanyEmail { get; set; } = string.Empty;
    [MaxLength(200)]
    public string CompanyWebsite { get; set; } = string.Empty;
    [MaxLength(500)]
    public string LogoUrl { get; set; } = string.Empty;

    [MaxLength(500)]
    public string BranchAddress { get; set; } = string.Empty;
    [MaxLength(50)]
    public string BranchPhone { get; set; } = string.Empty;

    [MaxLength(20)]
    public string PaperSize { get; set; } = "A4";
    [MaxLength(20)]
    public string Orientation { get; set; } = "portrait";
    [MaxLength(20)]
    public string FontSize { get; set; } = "medium";
    public int DefaultCopies { get; set; } = 1;

    [MaxLength(500)]
    public string FooterText { get; set; } = string.Empty;
    [MaxLength(1000)]
    public string TermsAndConditions { get; set; } = string.Empty;
    [MaxLength(200)]
    public string ThankYouMessage { get; set; } = string.Empty;
    [MaxLength(100)]
    public string AuthorizedBy { get; set; } = string.Empty;
    [MaxLength(200)]
    public string Signature { get; set; } = string.Empty;

    public bool ShowLogo { get; set; } = true;
    public bool ShowBranchInfo { get; set; } = true;
    public bool ShowPaymentDetails { get; set; } = true;
    public bool ShowAmountSummary { get; set; } = true;
    public bool ShowNotes { get; set; } = true;

    [MaxLength(10)]
    public string ReceiptNumberPrefix { get; set; } = "PAY-";
    [MaxLength(50)]
    public string PaymentReceiptTitle { get; set; } = "PAYMENT RECEIPT";
}
