namespace Domain.Entitites;

[Table("SalaryPayments", Schema = "finance")]
public class SalaryPayment : AuditableEntity<int>
{
    // Link to the financial ledger entry
    public Guid TransactionId { get; set; }
    public Transaction? Transaction { get; set; }

    // Employee reference
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    // Payroll breakdown
    [Column(TypeName = "decimal(10, 2)")]
    public decimal BasicSalary { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public decimal Bonus { get; set; } = 0;

    [Column(TypeName = "decimal(10, 2)")]
    public decimal Deduction { get; set; } = 0;

    // Payment period
    public int Month { get; set; }
    public int Year { get; set; }
}
