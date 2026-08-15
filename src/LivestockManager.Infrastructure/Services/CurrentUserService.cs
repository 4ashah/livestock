using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using LivestockManager.Domain.Common;
using LivestockManager.Infrastructure.Identity;
using LivestockManager.Infrastructure.Persistence;

namespace LivestockManager.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceProvider _serviceProvider;

    private const string CompanyIdCacheKey = "CUS_CompanyId";
    private const string IsSysAdminCacheKey = "CUS_IsSysAdmin";
    private const string AccessibleFarmIdsCacheKey = "CUS_AccessibleFarmIds";

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor,
        IServiceProvider serviceProvider)
    {
        _httpContextAccessor = httpContextAccessor;
        _serviceProvider = serviceProvider;
    }

    public Guid? UserId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var userId) ? userId : null;
        }
    }

    public Guid? CompanyId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return null;

            if (httpContext.Items.TryGetValue(CompanyIdCacheKey, out var cached))
                return (Guid?)cached;

            Guid? companyId = null;
            var userId = UserId;
            if (userId.HasValue)
            {
                using var scope = _serviceProvider.CreateScope();
                var scopedUserManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var user = scopedUserManager.FindByIdAsync(userId.Value.ToString()).GetAwaiter().GetResult();
                companyId = user?.CompanyId;
            }

            httpContext.Items[CompanyIdCacheKey] = companyId;
            return companyId;
        }
    }

    public bool IsSystemAdministrator
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return false;

            if (httpContext.Items.TryGetValue(IsSysAdminCacheKey, out var cached))
                return (bool)cached!;

            bool isSysAdmin = false;
            var user = httpContext.User;
            if (user.Identity?.IsAuthenticated == true)
            {
                isSysAdmin = user.IsInRole(RoleNames.SystemAdministrator);
                if (!isSysAdmin && UserId.HasValue)
                {
                    using var scope = _serviceProvider.CreateScope();
                    var scopedUserManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                    var appUser = scopedUserManager.FindByIdAsync(UserId.Value.ToString()).GetAwaiter().GetResult();
                    if (appUser != null)
                    {
                        isSysAdmin = scopedUserManager.IsInRoleAsync(appUser, RoleNames.SystemAdministrator).GetAwaiter().GetResult();
                    }
                }
            }

            httpContext.Items[IsSysAdminCacheKey] = isSysAdmin;
            return isSysAdmin;
        }
    }

    public async Task<List<Guid>> GetAccessibleFarmIdsAsync(CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null && httpContext.Items.TryGetValue(AccessibleFarmIdsCacheKey, out var cached))
            return (List<Guid>)cached!;

        List<Guid> farmIds = new();
        var userId = UserId;

        if (!userId.HasValue)
        {
            if (httpContext != null)
                httpContext.Items[AccessibleFarmIdsCacheKey] = farmIds;
            return farmIds;
        }

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var scopedUserManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var appUser = await scopedUserManager.FindByIdAsync(userId.Value.ToString());
        var userCompanyId = appUser?.CompanyId;
        var isSysAdmin = appUser != null && await scopedUserManager.IsInRoleAsync(appUser, RoleNames.SystemAdministrator);
        var isFarmManager = appUser != null && await scopedUserManager.IsInRoleAsync(appUser, RoleNames.FarmManager);

        if (isSysAdmin)
        {
            if (userCompanyId.HasValue)
            {
                farmIds = await dbContext.Farms
                    .Where(f => f.CompanyId == userCompanyId.Value)
                    .Select(f => f.Id)
                    .ToListAsync(cancellationToken);
            }
            else
            {
                farmIds = await dbContext.Farms
                    .Select(f => f.Id)
                    .ToListAsync(cancellationToken);
            }
        }
        else if (isFarmManager)
        {
            farmIds = await dbContext.Farms
                .Where(f => f.ManagerUserId == userId.Value)
                .Select(f => f.Id)
                .ToListAsync(cancellationToken);
        }
        else
        {
            if (userCompanyId.HasValue)
            {
                farmIds = await dbContext.Farms
                    .Where(f => f.CompanyId == userCompanyId.Value)
                    .Select(f => f.Id)
                    .ToListAsync(cancellationToken);
            }
        }

        if (httpContext != null)
            httpContext.Items[AccessibleFarmIdsCacheKey] = farmIds;

        return farmIds;
    }
}
