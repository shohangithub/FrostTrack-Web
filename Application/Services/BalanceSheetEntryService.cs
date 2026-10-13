using Application.Contractors;

namespace Application.Services;

public class BalanceSheetEntryService : IBalanceSheetEntryService
{
    private readonly IRepository<BalanceSheetEntry, Guid> _repository;
    private readonly IRepository<Company, int> _companyRepository;
    private readonly ICodeGenerationService _codeGenerationService;
    private readonly DefaultValueInjector _defaultValueInjector;

    public BalanceSheetEntryService(
        IRepository<BalanceSheetEntry, Guid> repository,
        IRepository<Company, int> companyRepository,
        ICodeGenerationService codeGenerationService,
        DefaultValueInjector defaultValueInjector)
    {
        _repository = repository;
        _companyRepository = companyRepository;
        _codeGenerationService = codeGenerationService;
        _defaultValueInjector = defaultValueInjector;
    }

    public async Task<IEnumerable<BalanceSheetEntryResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _repository.Query()
            .AsNoTracking()
            .OrderBy(x => x.EntryDate)
            .ThenBy(x => x.EntryType)
            .ThenBy(x => x.LineItem)
            .ThenBy(x => x.AccountName)
            .Select(x => new BalanceSheetEntryResponse(
                x.Id, x.Code, x.EntryDate, x.EntryType, x.LineItem, x.AccountName, x.Amount, x.ProfitCategory, x.Note))
            .ToListAsync(cancellationToken);
    }

    public async Task<BalanceSheetEntryResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : ToResponse(entity);
    }

    public async Task<BalanceSheetEntryResponse?> AddAsync(BalanceSheetEntryRequest request, CancellationToken cancellationToken = default)
    {
        await new BalanceSheetEntryValidator().ValidateAndThrowAsync(request, cancellationToken);

        var entity = new BalanceSheetEntry
        {
            Code = await _codeGenerationService.GenerateCodeAsync(_repository.Query(), "BSE", e => e.Code, cancellationToken),
            EntryType = request.EntryType,
            LineItem = request.LineItem,
            AccountName = request.AccountName.Trim(),
        };
        Apply(entity, request);
        _defaultValueInjector.InjectCreatingAudit<BalanceSheetEntry, Guid>(entity);

        var result = await _repository.AddAsync(entity, cancellationToken);
        return result ? ToResponse(entity) : null;
    }

    public async Task<BalanceSheetEntryResponse?> UpdateAsync(Guid id, BalanceSheetEntryRequest request, CancellationToken cancellationToken = default)
    {
        await new BalanceSheetEntryValidator().ValidateAndThrowAsync(request, cancellationToken);

        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Balance sheet entry with ID {id} not found");

        Apply(entity, request);
        _defaultValueInjector.InjectUpdatingAudit<BalanceSheetEntry, Guid>(entity);

        var result = await _repository.UpdateAsync(entity, cancellationToken);
        return result is null ? null : ToResponse(entity);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Balance sheet entry with ID {id} not found");
        return await _repository.DeleteAsync(entity, cancellationToken);
    }

    public async Task<BalanceSheetSettingsResponse> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        var company = await GetCompanyAsync(cancellationToken);
        var month = company?.FiscalYearStartMonth ?? 7;
        return new BalanceSheetSettingsResponse(company?.BalanceSheetStartDate, month is >= 1 and <= 12 ? month : 7);
    }

    public async Task<BalanceSheetSettingsResponse> UpdateSettingsAsync(BalanceSheetSettingsRequest request, CancellationToken cancellationToken = default)
    {
        await new BalanceSheetSettingsValidator().ValidateAndThrowAsync(request, cancellationToken);

        var company = await GetCompanyAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Company settings not found");

        company.BalanceSheetStartDate = request.BalanceSheetStartDate?.Date;
        company.FiscalYearStartMonth = request.FiscalYearStartMonth;
        await _companyRepository.UpdateAsync(company, cancellationToken);

        return new BalanceSheetSettingsResponse(company.BalanceSheetStartDate, company.FiscalYearStartMonth);
    }

    private Task<Company?> GetCompanyAsync(CancellationToken cancellationToken) =>
        _companyRepository.Query()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Id)
            .FirstOrDefaultAsync(cancellationToken);

    private static void Apply(BalanceSheetEntry entity, BalanceSheetEntryRequest request)
    {
        entity.EntryDate = request.EntryDate.Date;
        entity.EntryType = request.EntryType;
        entity.LineItem = request.LineItem;
        entity.AccountName = request.AccountName.Trim();
        entity.Amount = request.Amount;
        entity.ProfitCategory = request.EntryType == BalanceSheetEntryTypes.ADJUSTMENT ? request.ProfitCategory : null;
        entity.Note = request.Note;
    }

    private static BalanceSheetEntryResponse ToResponse(BalanceSheetEntry x) =>
        new(x.Id, x.Code, x.EntryDate, x.EntryType, x.LineItem, x.AccountName, x.Amount, x.ProfitCategory, x.Note);
}
