using Application.Framework;
using Domain.Entitites;

namespace Application.Repositories;

public interface IPaymentMethodRepository
{
    IQueryable<PaymentMethod> Query();
    Task<PaymentMethod?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PaymentMethod?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<PaymentMethodResponse> ManageUpdate(PaymentMethodRequest request, PaymentMethod? existingData, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}