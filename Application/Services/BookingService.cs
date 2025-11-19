namespace Application.Services;

public class BookingService : IBookingService
{
    private readonly IRepository<Booking, Guid> _repository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IRepository<Company, int> _companyRepository;
    private readonly IRepository<UnitConversion, int> _unitConversionRepository;
    private readonly DefaultValueInjector _defaultValueInjector;
    private readonly ITenantProvider _tenantProvider;
    private readonly Guid _tenantId;
    private readonly CurrentUser _currentUser;

    public BookingService(
        IRepository<Booking, Guid> repository,
        DefaultValueInjector defaultValueInjector,
        ITenantProvider tenantProvider,
        IUserContextService userContextService,
        IRepository<Company, int> companyRepository,
        IRepository<UnitConversion, int> unitConversionRepository,
        IBookingRepository bookingRepository)
    {
        _repository = repository;
        _defaultValueInjector = defaultValueInjector;
        _tenantProvider = tenantProvider;
        _tenantId = _tenantProvider.GetTenantId();
        _currentUser = userContextService.GetCurrentUser();
        _companyRepository = companyRepository;
        _unitConversionRepository = unitConversionRepository;
        _bookingRepository = bookingRepository;
    }

    public async Task<BookingResponse> AddAsync(BookingRequest request, CancellationToken cancellationToken = default)
    {
        var codeGenDependOn = await _companyRepository.Query().Select(x => x.CodeGeneration).FirstOrDefaultAsync();
        if (codeGenDependOn == ECodeGeneration.Branch)
        {
            BookingValidator validator = new(_repository, branchId: _currentUser.BranchId);
            await validator.ValidateAndThrowAsync(request, cancellationToken);
        }
        else
        {
            BookingValidator validator = new(_repository);
            await validator.ValidateAndThrowAsync(request, cancellationToken);
        }

        var entity = request.Adapt<Booking>();
        entity.BranchId = _currentUser.BranchId;
        _defaultValueInjector.InjectCreatingAudit<Booking, Guid>(entity);
        if (entity.BookingDetails != null && entity.BookingDetails.Any())
        {
            foreach (var detail in entity.BookingDetails)
            {
                detail.BillType = BillTypes.Monthly; // Set default BillType

                // Calculate BaseQuantity and BaseRate from unit conversion
                var unitConversion = await _unitConversionRepository.Query()
                    .FirstOrDefaultAsync(x => x.Id == detail.BookingUnitId, cancellationToken);

                if (unitConversion != null)
                {
                    detail.BaseQuantity = (decimal)(detail.BookingQuantity * unitConversion.ConversionValue);
                    detail.BaseRate = detail.BookingRate / (decimal)unitConversion.ConversionValue;
                }
                else
                {
                    detail.BaseQuantity = (decimal)detail.BookingQuantity;
                    detail.BaseRate = detail.BookingRate;
                }
            }
            _defaultValueInjector.InjectCreatingAudit<BookingDetail, Guid>(entity.BookingDetails.ToList());
        }

        await _repository.AddAsync(entity, cancellationToken);

        var response = entity.Adapt<BookingResponse>();
        return response;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _bookingRepository.DeleteAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteBatchAsync(List<Guid> ids, CancellationToken cancellationToken = default)
    {
        var result = await _repository.DeletableQuery(x => ids.Contains(x.Id)).ExecuteDeleteAsync(cancellationToken);
        return result > 0;
    }

    public async Task<BookingResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _repository.Query()
            .Include(x => x.BookingDetails)
            .ThenInclude(x => x.Product)
            .Include(x => x.BookingDetails)
            .ThenInclude(x => x.BookingUnit)
            .Include(x => x.Customer)
            .Include(x => x.Branch)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        var response = result is not null ? result.Adapt<BookingResponse>() : null;
        return response;
    }

    public async Task<IEnumerable<Lookup<Guid>>> GetLookup(Expression<Func<Booking, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var result = await _repository.Query()
            .Where(predicate)
            .Select(x => new Lookup<Guid>(x.Id, x.BookingNumber))
            .ToListAsync();
        return result;
    }

    public async Task<bool> IsExistsAsync(Guid id, CancellationToken cancellationToken = default)
        => await _repository.Query().AnyAsync(x => x.Id == id, cancellationToken);

    public async Task<BookingResponse> UpdateAsync(Guid id, BookingRequest request, CancellationToken cancellationToken = default)
    {
        BookingValidator validator = new(_repository, id);
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var existingData = await _bookingRepository.GetByIdAsync(id, cancellationToken);
        if (existingData == null) throw new Exception("Booking record not found!");

        existingData.BranchId = _currentUser.BranchId;
        existingData.BookingDate = request.BookingDate;
        existingData.CustomerId = request.CustomerId;
        existingData.Notes = request.Notes;

        _defaultValueInjector.InjectUpdatingAudit<Booking, Guid>(existingData);

        var response = await _bookingRepository.ManageUpdate(request, existingData, cancellationToken);

        return response;
    }

    public async Task<IEnumerable<BookingListResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var response = await _repository.Query()
           .Include(x => x.Customer)
           .Include(x => x.Branch)
           .Select(x => new BookingListResponse(
                x.Id,
                x.BookingNumber,
                x.BookingDate,
                x.CustomerId,
                x.Customer!,
                x.BranchId,
                x.Branch!,
                x.Notes,
                x.BookingDetails.Select(d => new BookingDetailListResponse(
                    d.Id,
                    d.Id,
                    d.ProductId,
                    d.Product!.ProductName,
                    d.BookingUnitId,
                    d.BookingUnit!.UnitName,
                    d.BookingQuantity,
                    d.BillType,
                    d.BookingRate,
                    d.BaseQuantity,
                    d.BaseRate))
               ))
           .ToListAsync(cancellationToken);
        return response;
    }

    public async Task<PaginationResult<BookingListResponse>> PaginationListAsync(PaginationQuery requestQuery, CancellationToken cancellationToken = default)
    {
        Expression<Func<Booking, bool>>? predicate = null;

        if (!string.IsNullOrEmpty(requestQuery.OpenText) && !string.IsNullOrWhiteSpace(requestQuery.OpenText))
        {
            predicate = obj => obj.BookingNumber.ToLower().Contains(requestQuery.OpenText.ToLower())
                            || (obj.Customer != null && obj.Customer.CustomerName.ToLower().Contains(requestQuery.OpenText.ToLower()));
        }

        Expression<Func<Booking, BookingListResponse>>? selector = x => new BookingListResponse(
            x.Id,
            x.BookingNumber,
            x.BookingDate,
            x.CustomerId,
            x.Customer!,
            x.BranchId,
            x.Branch!,
            x.Notes,
            x.BookingDetails.Select(d => new BookingDetailListResponse(
                d.Id,
                d.Id,
                d.ProductId,
                d.Product!.ProductName,
                d.BookingUnitId,
                d.BookingUnit!.UnitName,
                d.BookingQuantity,
                d.BillType,
                d.BookingRate,
                d.BaseQuantity,
                d.BaseRate))
            );

        var query = _bookingRepository.Query();

        return await _repository.PaginationQuery(query, paginationQuery: requestQuery, selector: selector, cancellationToken);
    }

    public async Task<string> GenerateBookingNumber(CancellationToken cancellationToken = default)
    {
        var currentDate = DateTime.Now;
        var year = currentDate.Year.ToString()?.Remove(0, 2);
        var month = currentDate.Month / 10 == 0 ? "0" + currentDate.Month : currentDate.Month.ToString();
        var dateString = $"{year}{month}";
        var dependOn = await _companyRepository.Query().Select(x => x.CodeGeneration).FirstOrDefaultAsync();

        if (dependOn == ECodeGeneration.Branch)
        {
            var code = long.Parse((await _repository.Query()
                .Where(x => x.BranchId == _currentUser.BranchId && x.BookingDate.Month == currentDate.Month)
                .OrderByDescending(x => x.BookingNumber)
                .Select(x => x.BookingNumber)
                .FirstOrDefaultAsync(cancellationToken))?.Remove(0, 6) ?? "0") + 1;

            if (code < 10)
                return $"BK{dateString}0000{code}";
            else if (code < 100)
                return $"BK{dateString}000{code}";
            else if (code < 1000)
                return $"BK{dateString}00{code}";
            else if (code < 10000)
                return $"BK{dateString}0{code}";
            else
                return $"BK{dateString}{code}";
        }
        else
        {
            var code = long.Parse((await _repository.Query()
                .OrderByDescending(x => x.BookingNumber)
                .Select(x => x.BookingNumber)
                .FirstOrDefaultAsync(cancellationToken))?.Remove(0, 6) ?? "0") + 1;

            if (code < 10)
                return $"BK{dateString}0000{code}";
            else if (code < 100)
                return $"BK{dateString}000{code}";
            else if (code < 1000)
                return $"BK{dateString}00{code}";
            else if (code < 10000)
                return $"BK{dateString}0{code}";
            else
                return $"BK{dateString}{code}";
        }
    }
}
