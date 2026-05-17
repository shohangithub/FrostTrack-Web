namespace Application.RequestDTO;

public class RecurringChargeRunRequest
{
    /// <summary>Reference date for cycle calculations. Defaults to UTC now when null.</summary>
    public DateTime? AsOfDate { get; set; }

    /// <summary>Optional operator note stored in the audit log.</summary>
    public string? Notes { get; set; }
}
