using Application.Framework;
using Application.RequestDTO;
using Application.ReponseDTO;
using Domain.Entitites;
using System.Linq.Expressions;

namespace Application.Contractors;

public interface IPaymentMethodService
{
    Task<IEnumerable<PaymentMethodListResponse>> ListAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<PaymentMethodListResponse>> GetActiveListAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<PaymentMethodListResponse>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);
    Task<PaginationResult<PaymentMethodListResponse>> PaginationListAsync(PaginationQuery requestQuery, CancellationToken cancellationToken = default);
    Task<PaymentMethodResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PaymentMethodResponse> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<PaymentMethodResponse> AddAsync(PaymentMethodRequest user, CancellationToken cancellationToken = default);
    Task<PaymentMethodResponse> UpdateAsync(int id, PaymentMethodRequest user, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> DeleteBatchAsync(List<int> ids, CancellationToken cancellationToken = default);
    Task<bool> IsExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<string> GenerateCode(CancellationToken cancellationToken = default);
    Task<IEnumerable<Lookup<int>>> GetLookup(Expression<Func<PaymentMethod, bool>> predicate, CancellationToken cancellationToken = default);
}