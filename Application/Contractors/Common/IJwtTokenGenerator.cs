using Domain.SubscriptionType;

namespace Application.Contractors.Common;

public interface IJwtTokenGenerator
{
    string GenerateToken(int id,
                         string firstName,
                         string lastName,
                         string email,
                         List<string>? permissions = null,
                         List<string>? roles = null,
                         SubscriptionType? subscriptionType = null);
}
