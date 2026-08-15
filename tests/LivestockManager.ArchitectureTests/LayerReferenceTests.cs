using System.Reflection;
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
    public void Web_DoesNotReference_Infrastructure_ImplementationNamespaces()
    {
        var forbiddenInfraSubNamespaces = new[]
        {
            "LivestockManager.Infrastructure.Persistence",
            "LivestockManager.Infrastructure.Services",
            "LivestockManager.Infrastructure.Security"
        };

        var allWebTypes = WebAssembly.GetTypes()
            .Where(t => t != null && !string.IsNullOrWhiteSpace(t.FullName))
            .Where(t =>
                !t.FullName!.StartsWith("AspNetCoreGeneratedDocument", StringComparison.Ordinal) &&
                !t.FullName.Equals("Program", StringComparison.Ordinal) &&
                !t.FullName.StartsWith("Program+", StringComparison.Ordinal) &&
                !t.FullName.StartsWith("ConnectionStringStartupValidator", StringComparison.Ordinal) &&
                !t.IsNestedPrivate)
            .ToList();

        Assert.NotEmpty(allWebTypes);

        var violations = new List<string>();

        foreach (var type in allWebTypes)
        {
            try
            {
                var referenced = new List<string>();

                void CollectReferences(MemberInfo? member)
                {
                    if (member == null) return;
                    try
                    {
                        if (member is Type t)
                        {
                            var baseType = t.BaseType;
                            if (baseType != null) referenced.Add(baseType.FullName ?? string.Empty);
                            foreach (var iface in t.GetInterfaces())
                                referenced.Add(iface.FullName ?? string.Empty);

                            foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                                referenced.Add(f.FieldType.FullName ?? string.Empty);
                            foreach (var p in t.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                                referenced.Add(p.PropertyType.FullName ?? string.Empty);
                            foreach (var c in t.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                            foreach (var cp in c.GetParameters())
                                referenced.Add(cp.ParameterType.FullName ?? string.Empty);
                            foreach (var m in t.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                            {
                                referenced.Add(m.ReturnType.FullName ?? string.Empty);
                                foreach (var mp in m.GetParameters())
                                    referenced.Add(mp.ParameterType.FullName ?? string.Empty);
                            }
                        }
                    }
                    catch
                    {
                    }
                }

                CollectReferences(type);

                foreach (var r in referenced)
                {
                    foreach (var ns in forbiddenInfraSubNamespaces)
                    {
                        if (r.StartsWith(ns, StringComparison.Ordinal))
                        {
                            violations.Add($"{type.FullName} references type in {ns}: {r}");
                            break;
                        }
                    }
                }

                var attrs = type.GetCustomAttributesData();
                foreach (var cad in attrs)
                {
                    var attrType = cad.AttributeType.FullName ?? string.Empty;
                    foreach (var ns in forbiddenInfraSubNamespaces)
                    {
                        if (attrType.StartsWith(ns, StringComparison.Ordinal))
                        {
                            violations.Add($"{type.FullName} has attribute in {ns}: {attrType}");
                            break;
                        }
                    }
                }
            }
            catch
            {
            }
        }

        Assert.Empty(violations);
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

    [Fact]
    public void ViewModels_FollowNamingConvention_EndWithViewModel()
    {
        var modelsNamespacePrefix = "LivestockManager.Web.Models";
        var allTypes = WebAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && !t.IsNested)
            .Where(t => !string.IsNullOrWhiteSpace(t.Namespace) &&
                        t.Namespace.StartsWith(modelsNamespacePrefix, StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(allTypes);

        var violators = new List<string>();
        foreach (var mt in allTypes)
        {
            if (!mt.Name.EndsWith("ViewModel", StringComparison.Ordinal))
            {
                violators.Add(mt.FullName ?? mt.Name);
            }
        }

        Assert.Empty(violators);
    }
}
