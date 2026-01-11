using Domain.Enums;
using Microsoft.Extensions.Caching.Memory;

namespace Application.Services;

public class CompanyService : ICompanyService
{
    private readonly IRepository<Company, int> _repository;
    private readonly IMemoryCache _cache;
    private readonly DefaultValueInjector _defaultValueInjector;
    private readonly ITenantProvider _tenantProvider;
    private const string CACHE_KEY = "CompanyCodeGenerationType";
    private const int CACHE_DURATION_MINUTES = 30;

    public CompanyService(
        IRepository<Company, int> repository,
        IMemoryCache cache,
        DefaultValueInjector defaultValueInjector,
        ITenantProvider tenantProvider)
    {
        _repository = repository;
        _cache = cache;
        _defaultValueInjector = defaultValueInjector;
        _tenantProvider = tenantProvider;
    }

    public async Task<IEnumerable<CompanyListResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var result = await _repository.Query()
            .AsNoTracking()
            .OrderByDescending(x => x.Id)
            .ToListAsync(cancellationToken);

        return result.Select(x => new CompanyListResponse(
            x.Id,
            x.Name,
            x.BusinessCurrency,
            x.CurrencySymbol,
            (int)x.CodeGeneration,
            x.CodeGeneration.ToString(),
            x.IsActive,
            x.Status
        ));
    }

    public async Task<CompanyResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await _repository.GetByIdAsync(id, cancellationToken);
        if (result is null) return null;

        return new CompanyResponse(
            result.Id,
            result.Name,
            result.LogoUrl,
            result.BusinessCurrency,
            result.CurrencySymbol,
            result.Description,
            result.AutoInvoicePrint,
            result.InvoiceHeader,
            result.InvoiceFooter,
            result.IsSingleBranch,
            (int)result.CodeGeneration,
            result.IsActive,
            result.Status
        );
    }

    public async Task<CompanyResponse> AddAsync(CompanyRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new Company
        {
            Name = request.Name,
            LogoUrl = request.LogoUrl ?? string.Empty,
            BusinessCurrency = request.BusinessCurrency ?? string.Empty,
            CurrencySymbol = request.CurrencySymbol ?? string.Empty,
            Description = request.Description ?? string.Empty,
            AutoInvoicePrint = request.AutoInvoicePrint,
            InvoiceHeader = request.InvoiceHeader ?? string.Empty,
            InvoiceFooter = request.InvoiceFooter ?? string.Empty,
            IsSingleBranch = request.IsSingleBranch,
            CodeGeneration = (ECodeGeneration)request.CodeGeneration,
            IsActive = request.IsActive
        };

        _defaultValueInjector.InjectTenant<Company, int>(entity);
        var result = await _repository.AddAsync(entity, cancellationToken);

        // Clear cache when company settings change
        _cache.Remove(CACHE_KEY);

        return result ? new CompanyResponse(
            entity.Id,
            entity.Name,
            entity.LogoUrl,
            entity.BusinessCurrency,
            entity.CurrencySymbol,
            entity.Description,
            entity.AutoInvoicePrint,
            entity.InvoiceHeader,
            entity.InvoiceFooter,
            entity.IsSingleBranch,
            (int)entity.CodeGeneration,
            entity.IsActive,
            entity.Status
        ) : null;
    }

    public async Task<CompanyResponse> UpdateAsync(int id, CompanyRequest request, CancellationToken cancellationToken = default)
    {
        var existingData = await _repository.GetByIdAsync(id, cancellationToken);
        if (existingData is null)
            throw new KeyNotFoundException($"Company with ID {id} not found");

        existingData.Name = request.Name;
        existingData.LogoUrl = request.LogoUrl ?? string.Empty;
        existingData.BusinessCurrency = request.BusinessCurrency ?? string.Empty;
        existingData.CurrencySymbol = request.CurrencySymbol ?? string.Empty;
        existingData.Description = request.Description ?? string.Empty;
        existingData.AutoInvoicePrint = request.AutoInvoicePrint;
        existingData.InvoiceHeader = request.InvoiceHeader ?? string.Empty;
        existingData.InvoiceFooter = request.InvoiceFooter ?? string.Empty;
        existingData.IsSingleBranch = request.IsSingleBranch;
        existingData.CodeGeneration = (ECodeGeneration)request.CodeGeneration;
        existingData.IsActive = request.IsActive;

        var result = await _repository.UpdateAsync(existingData, cancellationToken);

        // Clear cache when company settings change
        _cache.Remove(CACHE_KEY);

        return result is not null ? new CompanyResponse(
            result.Id,
            result.Name,
            result.LogoUrl,
            result.BusinessCurrency,
            result.CurrencySymbol,
            result.Description,
            result.AutoInvoicePrint,
            result.InvoiceHeader,
            result.InvoiceFooter,
            result.IsSingleBranch,
            (int)result.CodeGeneration,
            result.IsActive,
            result.Status
        ) : null;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var existingData = await _repository.GetByIdAsync(id, cancellationToken);
        if (existingData is null)
            throw new KeyNotFoundException($"Company with ID {id} not found");

        var result = await _repository.DeleteAsync(existingData, cancellationToken);

        // Clear cache when company is deleted
        if (result)
            _cache.Remove(CACHE_KEY);

        return result;
    }

    public async Task<PaginationResult<CompanyListResponse>> PaginationListAsync(
        PaginationQuery requestQuery,
        CancellationToken cancellationToken = default)
    {
        var query = _repository.Query().AsNoTracking();

        var totalRecords = await query.CountAsync(cancellationToken);

        query = (requestQuery.IsAscending ?? true)
            ? query.OrderBy(x => x.Name)
            : query.OrderByDescending(x => x.Name);

        var data = await query
            .Skip(requestQuery.PageIndex * requestQuery.PageSize)
            .Take(requestQuery.PageSize)
            .ToListAsync(cancellationToken);

        var result = data.Select(x => new CompanyListResponse(
            x.Id,
            x.Name,
            x.BusinessCurrency,
            x.CurrencySymbol,
            (int)x.CodeGeneration,
            x.CodeGeneration.ToString(),
            x.IsActive,
            x.Status
        ));

        var completeQuery = _repository.Query()
            .AsNoTracking()
            .OrderByDescending(x => x.Id)
            .Select(x => new CompanyListResponse(
                x.Id,
                x.Name,
                x.BusinessCurrency,
                x.CurrencySymbol,
                (int)x.CodeGeneration,
                x.CodeGeneration.ToString(),
                x.IsActive,
                x.Status
            ))
            .Skip(requestQuery.PageIndex * requestQuery.PageSize)
            .Take(requestQuery.PageSize);

        return await PaginationResult<CompanyListResponse>.CreateAsync(
            completeQuery,
            requestQuery.PageIndex,
            requestQuery.PageSize,
            cancellationToken
        );
    }
}
