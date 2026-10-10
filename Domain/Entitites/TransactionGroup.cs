namespace Domain.Entitites;

/// <summary>
/// Groups related transaction heads (e.g. "এম ডি স্যার গ্রহণ" and "এম ডি স্যার প্রদান")
/// so the Trial Balance shows them as one row with both Debit and Credit amounts.
/// </summary>
[Table("TransactionGroups", Schema = "finance")]
public class TransactionGroup : AuditableEntity<Guid>
{
    [Required]
    [MaxLength(100)]
    public required string Code { get; set; }

    [Required]
    [MaxLength(200)]
    public required string Name { get; set; }

    public int SortOrder { get; set; } = 0;

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    [NotMapped]
    public string Status => IsActive ? "Active" : "Inactive";
}
