namespace Application.ReponseDTO;

public class RecurringChargeEntryResponse
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public Guid BookingDetailId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public Guid? RecurringChargeRunId { get; set; }
    public string Source { get; set; } = string.Empty;   // INITIAL | RUN
    public DateTime BillPeriodFrom { get; set; }
    public DateTime BillPeriodTo { get; set; }
    public string BillType { get; set; } = string.Empty;
    public int Cycles { get; set; }
    public float Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}
