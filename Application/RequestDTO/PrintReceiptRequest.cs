namespace Application.RequestDTO
{
    public class PrintReceiptRequest
    {
        public int PaymentId { get; set; }
        public string PaperSize { get; set; } = "A4";
        public string Orientation { get; set; } = "portrait";
        public string FontSize { get; set; } = "medium";
        public int Copies { get; set; } = 1;
        public bool ShowLogo { get; set; } = true;
        public bool ShowBranchInfo { get; set; } = true;
        public string CustomFooterText { get; set; } = string.Empty;
    }

    public class PaymentReceiptData
    {
        public int Id { get; set; }
        public string PaymentNumber { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public string PaymentType { get; set; } = string.Empty;

        public SupplierInfo Supplier { get; set; } = new();
        public string PaymentMethod { get; set; } = string.Empty;
        public PaymentDetailsInfo? PaymentDetails { get; set; }

        public decimal PaymentAmount { get; set; }
        public decimal PreviousDue { get; set; }
        public decimal RemainingDue { get; set; }
        public string Notes { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public DateTime PrintDateTime { get; set; } = DateTime.Now;
    }

    public class SupplierInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class PaymentDetailsInfo
    {
        public string BankName { get; set; } = string.Empty;
        public string CheckNumber { get; set; } = string.Empty;
        public DateTime? CheckDate { get; set; }
        public string OnlinePaymentMethod { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public string GatewayReference { get; set; } = string.Empty;
        public string MobileWalletType { get; set; } = string.Empty;
        public string WalletNumber { get; set; } = string.Empty;
        public string WalletTransactionId { get; set; } = string.Empty;
        public string CardType { get; set; } = string.Empty;
        public string CardLastFour { get; set; } = string.Empty;
        public string CardTransactionId { get; set; } = string.Empty;
    }

    public class BookingInvoiceData
    {
        public Guid Id { get; set; }
        public string BookingNumber { get; set; } = string.Empty;
        public DateTime BookingDate { get; set; }
        public CustomerInfo Customer { get; set; } = null!;
        public BranchInfo Branch { get; set; } = null!;
        public string? Notes { get; set; }
        public List<BookingInvoiceDetail> BookingDetails { get; set; } = new();
        public decimal TotalAmount { get; set; }
        public decimal TotalQuantity { get; set; }
        public DateTime PrintDateTime { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class BookingInvoiceDetail
    {
        public int SerialNo { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
        public float BookingQuantity { get; set; }
        public string BillType { get; set; } = string.Empty;
        public decimal BookingRate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal BaseQuantity { get; set; }
        public decimal BaseRate { get; set; }
        public decimal DeliveryQuantity { get; set; }
    }

    public class CustomerInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class BranchInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
    }
}