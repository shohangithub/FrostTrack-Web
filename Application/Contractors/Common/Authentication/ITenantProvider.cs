namespace Application.Contractors.Authentication;

public interface ITenantProvider
{
    Guid GetTenantId();
}
