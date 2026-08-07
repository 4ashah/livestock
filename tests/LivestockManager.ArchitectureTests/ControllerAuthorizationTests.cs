using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using LivestockManager.Web.Controllers;
using Xunit;

namespace LivestockManager.ArchitectureTests;

public class ControllerAuthorizationTests
{
    private static readonly Assembly WebAssembly =
        typeof(HomeController).Assembly;

    [Fact]
    public void Controllers_NoHardcodedLegacyRoleStrings()
    {
        var forbidden = "Administrator,Manager";
        var controllers = GetControllerTypes();
        Assert.NotEmpty(controllers);

        var violations = new List<string>();

        foreach (var ctl in controllers)
        {
            var classAttrs = ctl.GetCustomAttributes<AuthorizeAttribute>(inherit: true);
            foreach (var a in classAttrs)
            {
                if (!string.IsNullOrWhiteSpace(a.Roles) && a.Roles.Contains(forbidden, StringComparison.Ordinal))
                {
                    violations.Add($"{ctl.FullName} class-level [Authorize(Roles=\"{a.Roles}\")]");
                }
            }

            var methods = ctl.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (var m in methods)
            {
                if (!typeof(IActionResult).IsAssignableFrom(m.ReturnType) &&
                    !typeof(Task<IActionResult>).IsAssignableFrom(m.ReturnType))
                    continue;

                var attrs = m.GetCustomAttributes<AuthorizeAttribute>(inherit: true);
                foreach (var a in attrs)
                {
                    if (!string.IsNullOrWhiteSpace(a.Roles) && a.Roles.Contains(forbidden, StringComparison.Ordinal))
                    {
                        violations.Add($"{ctl.FullName}.{m.Name} action-level [Authorize(Roles=\"{a.Roles}\")]");
                    }
                }
            }
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void PostActions_HaveExplicitAuthorize()
    {
        var controllers = GetControllerTypes();
        Assert.NotEmpty(controllers);

        var unprotectedPosts = new List<string>();

            foreach (var ctl in controllers)
            {
                bool classLevelAuth = ctl.GetCustomAttributes<AuthorizeAttribute>(inherit: true).Any()
                    || ctl.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).Any();

                var methods = ctl.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                foreach (var m in methods)
                {
                    if (!m.IsDefined(typeof(HttpPostAttribute), inherit: true))
                        continue;

                    bool returnsAction =
                        typeof(IActionResult).IsAssignableFrom(m.ReturnType) ||
                        (m.ReturnType.IsGenericType &&
                         m.ReturnType.GetGenericTypeDefinition() == typeof(Task<>) &&
                         typeof(IActionResult).IsAssignableFrom(m.ReturnType.GetGenericArguments()[0]));
                    if (!returnsAction) continue;

                    if (ctl.Name.Equals("AccountController", StringComparison.Ordinal)
                        && m.Name.Equals("Logout", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    bool allowAnon = m.IsDefined(typeof(AllowAnonymousAttribute), inherit: true);
                    if (allowAnon) continue;

                    bool actionAuth = m.IsDefined(typeof(AuthorizeAttribute), inherit: true);
                    var methodAuthorize = m.GetCustomAttributes<AuthorizeAttribute>(inherit: true).ToList();
                    bool hasExplicitAuthDetail = methodAuthorize.Any(a =>
                        !string.IsNullOrWhiteSpace(a.Roles) ||
                        !string.IsNullOrWhiteSpace(a.Policy));

                    bool classDetailOk = false;
                    var classAuthorize = ctl.GetCustomAttributes<AuthorizeAttribute>(inherit: true).ToList();
                    if (classAuthorize.Any(a =>
                        !string.IsNullOrWhiteSpace(a.Roles) ||
                        !string.IsNullOrWhiteSpace(a.Policy)))
                    {
                        classDetailOk = true;
                    }

                    if (!hasExplicitAuthDetail && !classDetailOk)
                    {
                        unprotectedPosts.Add($"{ctl.FullName}.{m.Name}");
                    }
                }
            }

            Assert.Empty(unprotectedPosts);
    }

    [Fact]
    public void AccountController_Login_OnlyEndpoint_AllowAnonymous()
    {
        var controllers = GetControllerTypes();
        var accountCtl = controllers.FirstOrDefault(c =>
            c.Name.Equals("AccountController", StringComparison.Ordinal));
        Assert.NotNull(accountCtl);

        var publicMethods = accountCtl!.GetMethods(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).ToList();
        Assert.NotEmpty(publicMethods);

        foreach (var m in publicMethods)
        {
            bool returnsAction =
                typeof(IActionResult).IsAssignableFrom(m.ReturnType) ||
                (m.ReturnType.IsGenericType &&
                 m.ReturnType.GetGenericTypeDefinition() == typeof(Task<>) &&
                 typeof(IActionResult).IsAssignableFrom(m.ReturnType.GetGenericArguments()[0]));
            if (!returnsAction) continue;

            bool isLogin = m.Name.StartsWith("Login", StringComparison.OrdinalIgnoreCase);
            bool isLogout = m.Name.StartsWith("Logout", StringComparison.OrdinalIgnoreCase);
            bool hasAllowAnon = m.IsDefined(typeof(AllowAnonymousAttribute), inherit: true);

            if (isLogin || isLogout)
            {
                if (isLogin)
                {
                    Assert.True(hasAllowAnon,
                        $"AccountController.{m.Name} Login method should have [AllowAnonymous].");
                }
                continue;
            }
            else
            {
                bool classLevelAnon = accountCtl.IsDefined(typeof(AllowAnonymousAttribute), inherit: true);
                bool hasAuthorize = m.IsDefined(typeof(AuthorizeAttribute), inherit: true)
                    || accountCtl.IsDefined(typeof(AuthorizeAttribute), inherit: true);

                if (!hasAllowAnon && !classLevelAnon)
                {
                    Assert.True(hasAuthorize,
                        $"AccountController.{m.Name} non-Login/Logout, non-anonymous endpoint should require authentication.");
                }
            }
        }
    }

    private List<Type> GetControllerTypes()
    {
        return WebAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Controller"))
            .ToList();
    }
}
