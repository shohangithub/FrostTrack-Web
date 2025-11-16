using Application.Repositories;
using Application.RequestDTO;
using Application.ReponseDTO;
using Application.Services.Common;
using Domain.Entitites;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Repositories;

public class PaymentMethodRepository : IPaymentMethodRepository
{
    private readonly ApplicationDbContext _context;
    private readonly DefaultValueInjector _defaultValueInjector;
    private readonly IRepository<PaymentMethod, int> _repository;

    public PaymentMethodRepository(ApplicationDbContext context, DefaultValueInjector defaultValueInjector, IRepository<PaymentMethod, int> repository)
    {
        _context = context;
        _defaultValueInjector = defaultValueInjector;
        _repository = repository;
    }

    public async Task<PaymentMethod?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.PaymentMethods
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<PaymentMethod?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.PaymentMethods
            .FirstOrDefaultAsync(p => p.Code == code, cancellationToken);
    }

    public IQueryable<PaymentMethod> Query()
    {
        return _context.PaymentMethods.AsNoTracking();
    }

    public async Task<PaymentMethodResponse> ManageUpdate(PaymentMethodRequest request, PaymentMethod? existingData, CancellationToken cancellationToken = default)
    {
        if (existingData is null)
        {
            var newEntity = request.Adapt<PaymentMethod>();
            _defaultValueInjector.InjectCreatingAudit<PaymentMethod, int>(newEntity);
            await _repository.AddAsync(newEntity, cancellationToken);
            return newEntity.Adapt<PaymentMethodResponse>();
        }
        else
        {
            request.Adapt(existingData);
            _defaultValueInjector.InjectUpdatingAudit<PaymentMethod, int>(existingData);
            await _repository.UpdateAsync(existingData, cancellationToken);
            return existingData.Adapt<PaymentMethodResponse>();
        }
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.PaymentMethods.FindAsync(id, cancellationToken);
        if (entity == null) return false;

        _context.PaymentMethods.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}