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