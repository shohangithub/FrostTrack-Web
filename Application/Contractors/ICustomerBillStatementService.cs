using Application.ReponseDTO;

namespace Application.Contractors;

public interface ICustomerBillStatementService
{
    /// <param name="fromUtc">Start of the first day (inclusive), in UTC.</param>
    /// <param name="toUtcExclusive">Start of the day after the last day (exclusive), in UTC.</param>
    Task<CustomerBillStatementResponse> GetStatementAsync(
        int customerId,
        DateTime fromUtc,
        DateTime toUtcExclusive,
        CancellationToken cancellationToken = default);
}
