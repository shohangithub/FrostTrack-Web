namespace Application.Contractors.Authentication;

public interface ICurrentUserProvider
{
    CurrentUser GetCurrentUser();
}
