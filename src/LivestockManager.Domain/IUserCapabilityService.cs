using System.Security.Claims;

namespace LivestockManager.Domain;

public interface IUserCapabilityService
{
    Task<bool> HasPermissionAsync(ClaimsPrincipal user, string permission);
    Task<bool> HasAnyPermissionAsync(ClaimsPrincipal user, IEnumerable<string> permissions);
    Task<List<string>> GetGrantedPermissionsAsync(ClaimsPrincipal user);
    Task<bool> CanAccessCompanyAsync(ClaimsPrincipal user, Guid companyId);
    Task<bool> CanAccessFarmAsync(ClaimsPrincipal user, Guid farmId);
}
