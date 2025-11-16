namespace Application.Services;

public class UserService : IUserService<int>
{
    private readonly IRepository<ApplicationUser, int> _repository;
    private readonly CurrentUser _currentUser;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    public UserService(IRepository<ApplicationUser, int> repository, IUserContextService userContextService, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
    {
        _repository = repository;
        _currentUser = userContextService.GetCurrentUser();
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<UserResponse> AddAsync(UserRequest user, CancellationToken cancellationToken = default)
    {
        UserValidator validator = new(_repository);
        await validator.ValidateAndThrowAsync(user, cancellationToken);



        var entity = user.Adapt<ApplicationUser>();
        entity.TenantId = _currentUser.TenantId;
        entity.BranchId = _currentUser.BranchId;

        var result = await _userManager.CreateAsync(entity);

        if (!result.Succeeded)
        {
            throw new Exception("Failed to create user: " + string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        // Assign the specified role to the new user
        if (!string.IsNullOrWhiteSpace(user.Role))
        {
            if (!await _roleManager.RoleExistsAsync(user.Role))
            {
                var role = new ApplicationRole { Name = user.Role };
                var roleResult = await _roleManager.CreateAsync(role);
                if (!roleResult.Succeeded)
                {
                    throw new Exception("Failed to create role: " + string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                }
            }
            await _userManager.AddToRoleAsync(entity, user.Role);
        }

        var roles = await _userManager.GetRolesAsync(entity);
        return new UserResponse(entity.Id, entity.UserName ?? string.Empty, entity.Email ?? string.Empty, roles, entity.IsActive, entity.IsActive ? "Active" : "Inactive");
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null) throw new ArgumentNullException(nameof(user));
        var result = await _userManager.DeleteAsync(user);
        return result.Succeeded;
    }

    public async Task<bool> DeleteBatchAsync(List<int> ids, CancellationToken cancellationToken = default)
    {
        //ProductCategoryValidator validator = new(_repository, id);
        //await validator.ValidateAndThrowAsync(user, cancellationToken);

        var deleted = 0;
        foreach (var id in ids)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user is null) continue;
            var r = await _userManager.DeleteAsync(user);
            if (r.Succeeded) deleted++;
        }
        return deleted > 0;
    }

    public async Task<UserResponse> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var u = await _userManager.FindByEmailAsync(email);
        if (u == null) return default!;
        var roles = await _userManager.GetRolesAsync(u);
        return new UserResponse(u.Id, u.UserName ?? string.Empty, u.Email ?? string.Empty, roles, u.IsActive, u.IsActive ? "Active" : "Inactive");
    }

    public async Task<UserResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var u = await _userManager.FindByIdAsync(id.ToString());
        if (u == null) return default!;
        var roles = await _userManager.GetRolesAsync(u);
        return new UserResponse(u.Id, u.UserName ?? string.Empty, u.Email ?? string.Empty, roles, u.IsActive, u.IsActive ? "Active" : "Inactive");
    }

    public async Task<IEnumerable<Lookup<int>>> GetLookup(Expression<Func<ApplicationUser, bool>> predicate, CancellationToken cancellationToken = default)
    {
        // For Identity-backed users we ignore the legacy predicate and return a lookup of users
        var list = await _userManager.Users.Select(x => new Lookup<int>(x.Id, x.UserName ?? string.Empty)).ToListAsync(cancellationToken);
        return list;
    }

    public async Task<bool> IsExistsAsync(int id, CancellationToken cancellationToken = default)
        => (await _userManager.FindByIdAsync(id.ToString())) != null;

    public async Task<IEnumerable<UserListResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userManager.Users.ToListAsync(cancellationToken);
        var result = new List<UserListResponse>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            result.Add(new UserListResponse(u.Id, u.UserName ?? string.Empty, u.Email ?? string.Empty, roles, u.IsActive ? "Active" : "Inactive"));
        }
        return result;
    }

    public async Task<PaginationResult<UserListResponse>> PaginationListAsync(PaginationQuery requestQuery, CancellationToken cancellationToken = default)
    {
        // Use Identity's UserManager store for pagination. We project ApplicationUser -> UserListResponse
        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrEmpty(requestQuery.OpenText) && !string.IsNullOrWhiteSpace(requestQuery.OpenText))
        {
            var term = requestQuery.OpenText.ToLower();
            query = query.Where(u => (u.UserName ?? string.Empty).ToLower().Contains(term) || (u.Email ?? string.Empty).ToLower().Contains(term));
        }

        // Get user list for pagination
        var users = await query
            .Select(u => new
            {
                u.Id,
                u.UserName,
                u.Email,
                u.IsActive
            })
            .ToListAsync(cancellationToken);

        // Get roles for each user
        var userList = new List<UserListResponse>();
        foreach (var user in users)
        {
            var appUser = await _userManager.FindByIdAsync(user.Id.ToString());
            // guard against null (user may have been removed)
            if (appUser is null)
            {
                userList.Add(new UserListResponse(user.Id, user.UserName ?? string.Empty, user.Email ?? string.Empty, new List<string>(), (user.IsActive ? "Active" : "Inactive")));
                continue;
            }
            var roles = await _userManager.GetRolesAsync(appUser);
            userList.Add(new UserListResponse(user.Id, user.UserName ?? string.Empty, user.Email ?? string.Empty, roles, (user.IsActive ? "Active" : "Inactive")));
        }

        // Convert to IQueryable for pagination
        var userListQuery = userList.AsQueryable();
        return await PaginationResult<UserListResponse>.CreateAsync(userListQuery, requestQuery.PageIndex, requestQuery.PageSize, cancellationToken);
    }

    public async Task<UserResponse> UpdateAsync(int id, UserRequest user, CancellationToken cancellationToken = default)
    {
        var appUser = await _userManager.FindByIdAsync(id.ToString());
        if (appUser is null) throw new ArgumentNullException(nameof(appUser));

        // Update basic properties
        appUser.UserName = user.UserName;
        appUser.Email = user.Email;
        appUser.IsActive = user.IsActive;

        var updateResult = await _userManager.UpdateAsync(appUser);
        if (!updateResult.Succeeded)
        {
            throw new Exception("Failed to update user: " + string.Join(", ", updateResult.Errors.Select(e => e.Description)));
        }

        // Handle role changes: remove existing roles and add the new one (single-role model)
        if (!string.IsNullOrWhiteSpace(user.Role))
        {
            var currentRoles = await _userManager.GetRolesAsync(appUser);
            if (!currentRoles.Contains(user.Role, StringComparer.OrdinalIgnoreCase))
            {
                // ensure role exists
                if (!await _roleManager.RoleExistsAsync(user.Role))
                {
                    var role = new ApplicationRole { Name = user.Role };
                    var roleResult = await _roleManager.CreateAsync(role);
                    if (!roleResult.Succeeded)
                        throw new Exception("Failed to create role: " + string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                }
                if (currentRoles.Any()) await _userManager.RemoveFromRolesAsync(appUser, currentRoles);
                await _userManager.AddToRoleAsync(appUser, user.Role);
            }
        }

        var roles = await _userManager.GetRolesAsync(appUser);
        return new UserResponse(appUser.Id, appUser.UserName ?? string.Empty, appUser.Email ?? string.Empty, roles, appUser.IsActive, appUser.IsActive ? "Active" : "Inactive");
    }
}
