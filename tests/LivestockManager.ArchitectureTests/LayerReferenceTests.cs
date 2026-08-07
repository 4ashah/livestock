using NetArchTest.Rules;
using LivestockManager.Web.Controllers;
using Xunit;

namespace LivestockManager.ArchitectureTests;

public class LayerReferenceTests
{
    private static readonly System.Reflection.Assembly DomainAssembly =
        typeof(LivestockManager.Domain.Common.RoleNames).Assembly;
    private static readonly System.Reflection.Assembly ApplicationAssembly =
        typeof(LivestockManager.Application.ServiceCollectionExtensions).Assembly;
    private static readonly System.Reflection.Assembly InfrastructureAssembly =
        typeof(LivestockManager.Infrastructure.ServiceCollectionExtensions).Assembly;
    private static readonly System.Reflection.Assembly WebAssembly =
        typeof(HomeController).Assembly;

    [Fact]
    public void Domain_DoesNotReference_Infrastructure()
    {
        var result = Types
            .InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("LivestockManager.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Domain types must not reference Infrastructure namespace. Violating: " +
            $"{string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? new[] { "none" }.Take(10))}");
    }

    [Fact]
    public void Domain_DoesNotReference_Web()
    {
        var result = Types
            .InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("LivestockManager.Web")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Domain types must not reference Web namespace. Violating: " +
            $"{string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? new[] { "none" }.Take(10))}");
    }

    [Fact]
    public void Application_DoesNotReference_Web()
    {
        var result = Types
            .InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("LivestockManager.Web")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Application types must not reference Web namespace. Violating: " +
            $"{string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? new[] { "none" }.Take(10))}");
    }

    [Fact]
    public void ProductionProjects_Never_Reference_TestProjects()
    {
        var testNamespaces = new[]
        {
            "LivestockManager.UnitTests",
            "LivestockManager.IntegrationTests",
            "LivestockManager.ArchitectureTests",
            "LivestockManager.EndToEndTests"
        };

        var productionAssemblies = new[]
        {
            DomainAssembly, ApplicationAssembly, InfrastructureAssembly, WebAssembly
        };

        foreach (var asm in productionAssemblies)
        {
            var referencedNames = asm.GetReferencedAssemblies()
                .Select(a => a.Name ?? string.Empty)
                .ToList();

            foreach (var tn in testNamespaces)
            {
                Assert.DoesNotContain(referencedNames, n =>
                    n.Equals(tn, StringComparison.OrdinalIgnoreCase));
            }
        }
    }

    [Fact]
    public void Controllers_Inherit_FromControllerBase()
    {
        var controllerTypes = Types
            .InAssembly(WebAssembly)
            .That()
            .HaveNameEndingWith("Controller")
            .And()
            .AreClasses()
            .GetTypes()
            .ToList();

        Assert.NotEmpty(controllerTypes);

        foreach (var ctl in controllerTypes)
        {
            bool inheritsFromControllerBase = false;
            var t = ctl;
            while (t != null && t != typeof(object))
            {
                if (t.FullName != null &&
                    (t.FullName.Contains("ControllerBase") ||
                     t.FullName.Contains("Controller")))
                {
                    inheritsFromControllerBase = true;
                    break;
                }
                t = t.BaseType;
            }

            Assert.True(inheritsFromControllerBase,
                $"Controller {ctl.FullName} should inherit from Controller or ControllerBase.");
        }
    }
}
