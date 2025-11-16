namespace Domain.Entitites;

[Table("Employees")]
public class Employee : AuditableEntity<int>
{
    public required string EmployeeName { get; set; }
    public required string EmployeeCode { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public DateTime? JoiningDate { get; set; }
    public string? Department { get; set; }
    public string? Designation { get; set; }
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Salary { get; set; }
    public string? EmploymentType { get; set; }
    public string? EmergencyContact { get; set; }
    public string? BloodGroup { get; set; }
    public string? NationalId { get; set; }
    public string? PassportNumber { get; set; }
    public string? BankAccount { get; set; }
    public string? Notes { get; set; }
    public string? PhotoUrl { get; set; }
    public required bool IsActive { get; set; } = true;
    [NotMapped]
    public string Status => IsActive ? "Active" : "Inactive";

    public int? BranchId { get; set; }

    // Navigation properties for future employee relations
    // public ICollection<EmployeeLeave> EmployeeLeaves { get; set; } = [];
    // public ICollection<EmployeeSalary> EmployeeSalaries { get; set; } = [];
}