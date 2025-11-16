namespace Application.Services.Common;

public class DefaultValueInjector
{
    private readonly ITenantProvider _tenantProvider;
    private readonly Guid _tenantId;
    private readonly CurrentUser _currentUser;
    public DefaultValueInjector(ITenantProvider tenantProvider, IUserContextService userContextService)
    {
        _tenantProvider = tenantProvider;
        _tenantId = _tenantProvider.GetTenantId();
        _currentUser = userContextService.GetCurrentUser();
    }

    public T InjectTenant<T, Key>(T model) where T : BaseEntity<Key>
    {
        model.TenantId = _tenantId;
        return model;
    }

    public T InjectCreatingAudit<T, Key>(T model) where T : AuditableEntity<Key>
    {
        model.TenantId = _tenantId;
        if (model.CreatedById == 0)
            model.CreatedById = _currentUser.Id;
        model.CreatedTime = DateTime.UtcNow;

        return model;
    }

    public T InjectUpdatingAudit<T, Key>(T model) where T : AuditableEntity<Key>
    {
        model.TenantId = _tenantId;
        model.LastUpdatedById = _currentUser.Id;
        model.LastUpdatedTime = DateTime.UtcNow;

        return model;
    }

    public List<T> InjectTenant<T, Key>(List<T> entities) where T : BaseEntity<Key>
    {
        foreach (var entity in entities)
        {
            InjectTenant<T, Key>(entity);
        }
        return entities;
    }

    public List<T> InjectCreatingAudit<T, Key>(List<T> entities) where T : AuditableEntity<Key>
    {
        foreach (var entity in entities)
        {
            InjectCreatingAudit<T, Key>(entity);
        }
        return entities;
    }

    public List<T> InjectUpdatingAudit<T, Key>(List<T> entities) where T : AuditableEntity<Key>
    {
        foreach (var entity in entities)
        {
            InjectUpdatingAudit<T, Key>(entity);
        }
        return entities;
    }
}
