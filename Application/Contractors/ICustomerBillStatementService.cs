using Application.ReponseDTO;

namespace Application.Contractors;

public interface ICustomerBillStatementService
{
    /// <summary>Customers that have at least one active contract (চুক্তি) booking.</summary>
    Task<List<Lookup<int>>> GetContractCustomersAsync(CancellationToken cancellationToken = default);

    /// <param name="fromUtc">Start of the first day (inclusive), in UTC.</param>
    /// <param name="toUtcExclusive">Start of the day after the last day (exclusive), in UTC.</param>
    Task<CustomerBillStatementResponse> GetStatementAsync(
        int customerId,
        DateTime fromUtc,
        DateTime toUtcExclusive,
        CancellationToken cancellationToken = default);
}
