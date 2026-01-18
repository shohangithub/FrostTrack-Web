using Application.Contractors;
using Application.Contractors.Authentication;
using Application.Framework;
using Application.RequestDTO;
using Application.ReponseDTO;
using Domain;
using Domain.Entitites;

namespace Application.Services;

public class DeliveryChallanService : IDeliveryChallanService
{
    private readonly IRepository<DeliveryChallan, Guid> _repository;
    private readonly IRepository<Delivery, Guid> _deliveryRepository;
    private readonly IRepository<DeliveryChallanItem, Guid> _itemRepository;
    private readonly DefaultValueInjector _defaultValueInjector;
    private readonly ITenantProvider _tenantProvider;
    private readonly IUserContextService _userContextService;
    private readonly Guid _tenantId;
    private readonly CurrentUser _currentUser;

    public DeliveryChallanService(
        IRepository<DeliveryChallan, Guid> repository,
        IRepository<Delivery, Guid> deliveryRepository,
        IRepository<DeliveryChallanItem, Guid> itemRepository,
        DefaultValueInjector defaultValueInjector,
        ITenantProvider tenantProvider,
        IUserContextService userContextService)
    {
        _repository = repository;
        _deliveryRepository = deliveryRepository;
        _itemRepository = itemRepository;
        _defaultValueInjector = defaultValueInjector;
        _tenantProvider = tenantProvider;
        _userContextService = userContextService;
        _tenantId = _tenantProvider.GetTenantId();
        _currentUser = _userContextService.GetCurrentUser();
    }

    public async Task<IEnumerable<DeliveryChallanListResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var response = await _repository.Query()
            .Include(x => x.ChallanItems)
            .ThenInclude(x => x.Delivery)
            .ThenInclude(x => x.Booking)
            .ThenInclude(x => x.Customer)
            .Where(x => !x.IsDeleted)
            .Select(x => new DeliveryChallanListResponse(
                x.Id,
                x.ChallanNumber,
                x.ChallanDate,
                x.VehicleNumber,
                x.DriverName,
                x.Destination,
                x.Status,
                x.ChallanItems.Count,
                x.ChallanItems.Sum(i => i.Delivery.ChargeAmount),
                x.DispatchTime,
                x.DeliveryTime
            ))
            .ToListAsync(cancellationToken);

        return response;
    }

    public async Task<PaginationResult<DeliveryChallanListResponse>> PaginationListAsync(
        PaginationQuery requestQuery,
        CancellationToken cancellationToken = default)
    {
        Expression<Func<DeliveryChallan, bool>>? predicate = x => !x.IsDeleted;

        if (!string.IsNullOrEmpty(requestQuery.OpenText) && !string.IsNullOrWhiteSpace(requestQuery.OpenText))
        {
            predicate = obj => !obj.IsDeleted &&
                (obj.ChallanNumber.ToLower().Contains(requestQuery.OpenText.ToLower()) ||
                 obj.VehicleNumber.ToLower().Contains(requestQuery.OpenText.ToLower()) ||
                 obj.DriverName.ToLower().Contains(requestQuery.OpenText.ToLower()));
        }

        Expression<Func<DeliveryChallan, DeliveryChallanListResponse>>? selector = x => new DeliveryChallanListResponse(
            x.Id,
            x.ChallanNumber,
            x.ChallanDate,
            x.VehicleNumber,
            x.DriverName,
            x.Destination,
            x.Status,
            x.ChallanItems.Count,
            x.ChallanItems.Sum(i => i.Delivery.ChargeAmount),
            x.DispatchTime,
            x.DeliveryTime
        );

        var query = _repository.Query();

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        return await _repository.PaginationQuery(query, paginationQuery: requestQuery, selector: selector, cancellationToken);
    }

    public async Task<DeliveryChallanResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var challan = await _repository.Query()
            .Include(x => x.ChallanItems)
            .ThenInclude(x => x.Delivery)
            .ThenInclude(x => x.DeliveryDetails)
            .ThenInclude(x => x.BookingDetail)
            .ThenInclude(x => x.Product)
            .Include(x => x.ChallanItems)
            .ThenInclude(x => x.Delivery)
            .ThenInclude(x => x.DeliveryDetails)
            .ThenInclude(x => x.DeliveryUnit)
            .Include(x => x.ChallanItems)
            .ThenInclude(x => x.Delivery)
            .ThenInclude(x => x.Booking)
            .ThenInclude(x => x.Customer)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Delivery Challan with ID {id} not found");

        return MapToResponse(challan);
    }

    public async Task<DeliveryChallanResponse> AddAsync(
        DeliveryChallanRequest request,
        CancellationToken cancellationToken = default)
    {
        // Validate deliveries exist
        var deliveries = await _deliveryRepository.Query()
            .Where(x => request.DeliveryIds.Contains(x.Id) && !x.IsDeleted)
            .ToListAsync(cancellationToken);

        if (deliveries.Count != request.DeliveryIds.Count)
        {
            throw new ArgumentException("One or more delivery IDs are invalid");
        }

        var challanId = Guid.NewGuid();

        var challan = new DeliveryChallan
        {
            Id = challanId,
            ChallanNumber = request.ChallanNumber,
            ChallanDate = request.ChallanDate,
            VehicleNumber = request.VehicleNumber,
            DriverName = request.DriverName,
            DriverContact = request.DriverContact,
            VehicleType = request.VehicleType,
            TransportCompany = request.TransportCompany,
            Destination = request.Destination,
            BranchId = _currentUser.BranchId,
            Remarks = request.Remarks,
            Status = request.Status,
            DispatchTime = request.DispatchTime,
            DeliveryTime = request.DeliveryTime,
            ChallanItems = request.DeliveryIds.Select(deliveryId => new DeliveryChallanItem
            {
                Id = Guid.NewGuid(),
                DeliveryChallanId = challanId,
                DeliveryId = deliveryId
            }).ToList()
        };

        _defaultValueInjector.InjectCreatingAudit<DeliveryChallan, Guid>(challan);

        await _repository.AddAsync(challan, cancellationToken);

        return await GetByIdAsync(challan.Id, cancellationToken);
    }

    public async Task<DeliveryChallanResponse> UpdateAsync(
        Guid id,
        DeliveryChallanRequest request,
        CancellationToken cancellationToken = default)
    {
        var challan = await _repository.Query()
            .Include(x => x.ChallanItems)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Delivery Challan with ID {id} not found");

        // Validate deliveries exist
        var deliveries = await _deliveryRepository.Query()
            .Where(x => request.DeliveryIds.Contains(x.Id) && !x.IsDeleted)
            .ToListAsync(cancellationToken);

        if (deliveries.Count != request.DeliveryIds.Count)
        {
            throw new ArgumentException("One or more delivery IDs are invalid");
        }

        challan.ChallanNumber = request.ChallanNumber;
        challan.ChallanDate = request.ChallanDate;
        challan.VehicleNumber = request.VehicleNumber;
        challan.DriverName = request.DriverName;
        challan.DriverContact = request.DriverContact;
        challan.VehicleType = request.VehicleType;
        challan.TransportCompany = request.TransportCompany;
        challan.Destination = request.Destination;
        challan.Remarks = request.Remarks;
        challan.Status = request.Status;
        challan.DispatchTime = request.DispatchTime;
        challan.DeliveryTime = request.DeliveryTime;

        // Remove old items
        if (challan.ChallanItems != null)
        {
            foreach (var item in challan.ChallanItems.ToList())
            {
                await _itemRepository.DeleteAsync(item, cancellationToken);
            }
        }

        // Add new items
        challan.ChallanItems = request.DeliveryIds.Select(deliveryId => new DeliveryChallanItem
        {
            Id = Guid.NewGuid(),
            DeliveryChallanId = id,
            DeliveryId = deliveryId
        }).ToList();

        _defaultValueInjector.InjectUpdatingAudit<DeliveryChallan, Guid>(challan);

        await _repository.UpdateAsync(challan, cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var challan = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Delivery Challan with ID {id} not found");

        challan.IsDeleted = true;
        challan.DeletedAt = DateTime.UtcNow;
        challan.DeletedById = _currentUser.Id;

        await _repository.UpdateAsync(challan, cancellationToken);
        return true;
    }

    public async Task<bool> DeleteBatchAsync(List<Guid> ids, CancellationToken cancellationToken = default)
    {
        foreach (var id in ids)
        {
            await DeleteAsync(id, cancellationToken);
        }
        return true;
    }

    public async Task<bool> IsExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _repository.Query().AnyAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
    }

    public async Task<string> GenerateChallanNumber(CancellationToken cancellationToken = default)
    {
        var lastChallan = await _repository.Query()
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.ChallanNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (lastChallan != null)
        {
            var numberPart = ExtractNumberFromCode(lastChallan.ChallanNumber);
            return $"CH-{(numberPart + 1):D6}";
        }

        return "CH-000001";
    }

    public async Task<DeliveryChallanResponse> UpdateStatusAsync(
        Guid id,
        string status,
        CancellationToken cancellationToken = default)
    {
        var challan = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Delivery Challan with ID {id} not found");

        challan.Status = status;

        if (status == "In Transit" && challan.DispatchTime == null)
        {
            challan.DispatchTime = DateTime.UtcNow;
        }
        else if (status == "Delivered" && challan.DeliveryTime == null)
        {
            challan.DeliveryTime = DateTime.UtcNow;
        }

        _defaultValueInjector.InjectUpdatingAudit<DeliveryChallan, Guid>(challan);

        await _repository.UpdateAsync(challan, cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    private static int ExtractNumberFromCode(string code)
    {
        var match = System.Text.RegularExpressions.Regex.Match(code, @"\d+");
        return match.Success ? int.Parse(match.Value) : 0;
    }

    private static DeliveryChallanResponse MapToResponse(DeliveryChallan challan)
    {
        return new DeliveryChallanResponse(
            challan.Id,
            challan.ChallanNumber,
            challan.ChallanDate,
            challan.VehicleNumber,
            challan.DriverName,
            challan.DriverContact,
            challan.VehicleType,
            challan.TransportCompany,
            challan.Destination,
            challan.BranchId,
            challan.Remarks,
            challan.Status,
            challan.DispatchTime,
            challan.DeliveryTime,
            challan.ChallanItems.Select(item => new DeliveryChallanItemResponse(
                item.Id,
                item.DeliveryChallanId,
                item.DeliveryId,
                item.Delivery?.DeliveryNumber ?? "",
                item.Delivery?.DeliveryDate ?? DateTime.MinValue,
                item.Delivery?.Booking?.BookingNumber ?? "",
                item.Delivery?.Booking?.Customer?.CustomerName ?? "",
                item.Delivery?.ChargeAmount ?? 0,
                item.Notes,
                item.Delivery?.DeliveryDetails?.Select(detail => new DeliveryChallanItemDetailResponse(
                    detail.BookingDetail?.Product?.ProductName ?? "",
                    detail.DeliveryQuantity,
                    detail.DeliveryUnit?.UnitName ?? ""
                )).ToList() ?? new List<DeliveryChallanItemDetailResponse>()
            )).ToList()
        );
    }
}
