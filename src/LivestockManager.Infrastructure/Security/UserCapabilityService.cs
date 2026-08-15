using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using LivestockManager.Domain;
using LivestockManager.Domain.Common;
using LivestockManager.Infrastructure.Identity;
using LivestockManager.Infrastructure.Persistence;
using LivestockManager.Infrastructure.Services;

namespace LivestockManager.Infrastructure.Security;

public class UserCapabilityService : IUserCapabilityService
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    private const string PermissionCachePrefix = "UCPerm_";
    private const string GrantedPermissionsCacheKey = "UCGrantedPermissions";
    private const string UserCompanyIdCacheKey = "UCUserCompanyId";
    private const string FarmCompanyIdPrefix = "UCFarmCompany_";

    public UserCapabilityService(
        IAuthorizationService authorizationService,
        IHttpContextAccessor httpContextAccessor,
        UserManager<ApplicationUser> userManager,
        AppDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _authorizationService = authorizationService;
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<bool> HasPermissionAsync(ClaimsPrincipal user, string permission)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var cacheKey = PermissionCachePrefix + permission;

        if (httpContext != null && httpContext.Items.TryGetValue(cacheKey, out var cached))
        {
            return (bool)cached!;
        }

        var result = await _authorizationService.AuthorizeAsync(user, permission);
        var granted = result.Succeeded;

        if (httpContext != null)
        {
            httpContext.Items[cacheKey] = granted;
        }

        return granted;
    }

    public async Task<bool> HasAnyPermissionAsync(ClaimsPrincipal user, IEnumerable<string> permissions)
    {
        foreach (var permission in permissions)
        {
            if (await HasPermissionAsync(user, permission))
            {
                return true;
            }
        }
        return false;
    }

    public async Task<List<string>> GetGrantedPermissionsAsync(ClaimsPrincipal user)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext != null && httpContext.Items.TryGetValue(GrantedPermissionsCacheKey, out var cached))
        {
            return (List<string>)cached!;
        }

        var granted = new List<string>();

        foreach (var permission in PermissionNames.All)
        {
            if (await HasPermissionAsync(user, permission))
            {
                granted.Add(permission);
            }
        }

        if (httpContext != null)
        {
            httpContext.Items[GrantedPermissionsCacheKey] = granted;
        }

        return granted;
    }

    public async Task<bool> CanAccessCompanyAsync(ClaimsPrincipal user, Guid companyId)
    {
        if (user == null || user.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        if (companyId == Guid.Empty)
        {
            return false;
        }

        if (_currentUserService.IsSystemAdministrator)
        {
            return true;
        }

        var userCompanyId = _currentUserService.CompanyId;

        if (!userCompanyId.HasValue)
        {
            userCompanyId = await GetUserCompanyIdAsync(user);
        }

        return userCompanyId.HasValue && userCompanyId.Value == companyId;
    }

    public async Task<bool> CanAccessFarmAsync(ClaimsPrincipal user, Guid farmId)
    {
        if (user == null || user.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        if (farmId == Guid.Empty)
        {
            return false;
        }

        if (_currentUserService.IsSystemAdministrator)
        {
            return true;
        }

        var accessibleFarmIds = await _currentUserService.GetAccessibleFarmIdsAsync(CancellationToken.None);
        if (accessibleFarmIds.Contains(farmId))
        {
            return true;
        }

        var farmCompanyId = await GetFarmCompanyIdAsync(farmId);
        if (!farmCompanyId.HasValue)
        {
            return false;
        }

        var userCompanyId = _currentUserService.CompanyId;
        if (!userCompanyId.HasValue)
        {
            userCompanyId = await GetUserCompanyIdAsync(user);
        }

        return userCompanyId.HasValue && userCompanyId.Value == farmCompanyId.Value;
    }

    private async Task<Guid?> GetUserCompanyIdAsync(ClaimsPrincipal user)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext != null && httpContext.Items.TryGetValue(UserCompanyIdCacheKey, out var cached))
        {
            return (Guid?)cached;
        }

        Guid? companyId = _currentUserService.CompanyId;

        if (!companyId.HasValue)
        {
            var appUser = await _userManager.GetUserAsync(user);
            if (appUser != null)
            {
                companyId = appUser.CompanyId;
            }
        }

        if (httpContext != null)
        {
            httpContext.Items[UserCompanyIdCacheKey] = companyId;
        }

        return companyId;
    }

    private async Task<Guid?> GetFarmCompanyIdAsync(Guid farmId)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var cacheKey = FarmCompanyIdPrefix + farmId.ToString("N");

        if (httpContext != null && httpContext.Items.TryGetValue(cacheKey, out var cached))
        {
            return (Guid?)cached;
        }

        var farmCompanyId = await _dbContext.Farms
            .Where(f => f.Id == farmId)
            .Select(f => (Guid?)f.CompanyId)
            .FirstOrDefaultAsync();

        if (httpContext != null)
        {
            httpContext.Items[cacheKey] = farmCompanyId;
        }

        return farmCompanyId;
    }
}
