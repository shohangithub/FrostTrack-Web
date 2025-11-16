namespace Domain.Entitites;
public abstract class BaseEntity<T>
{
    [Key, Column(Order = 0)]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public T? Id { get; set; }
    public Guid TenantId { get; set; }
}
