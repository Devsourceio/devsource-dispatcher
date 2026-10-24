using System.Reflection;
using System.Reflection.Emit;
using DevSource.Dispatcher.Engine;
using DevSource.Dispatcher.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace DevSource.Dispatcher.Tests;

public sealed class DispatcherDiscoveryTests
{
    private static readonly Assembly TestAssembly = typeof(DispatcherDiscoveryTests).Assembly;
    private static readonly Assembly FixtureAssembly = typeof(FixtureCommand).Assembly;

    [Fact]
    public void FindCandidateAssemblies_WithNullRoot_ShouldThrow()
    {
        var action = () => DispatcherDiscovery.FindCandidateAssemblies(null!);

        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void FindCandidateAssemblies_ShouldIncludeReferencedDispatcherAssemblies()
    {
        var assemblies = DispatcherDiscovery.FindCandidateAssemblies(TestAssembly);

        Assert.Contains(TestAssembly, assemblies);
        Assert.Contains(FixtureAssembly, assemblies);
    }

    [Fact]
    public void FindCandidateAssemblies_ShouldIncludeLoadedDispatcherAssembliesEvenWhenRootDoesNotReferenceThem()
    {
        var root = CreateMockRoot();

        var assemblies = DispatcherDiscovery.FindCandidateAssemblies(root);

        Assert.Contains(root, assemblies);
        Assert.Contains(TestAssembly, assemblies);
        Assert.Contains(FixtureAssembly, DispatcherDiscovery.FindCandidateAssemblies(FixtureAssembly));
    }

    [Fact]
    public void FindCandidateAssemblies_ShouldSkipBlankAndFrameworkReferencesWithoutLoadingThem()
    {
        var root = CreateMockRoot(
            new AssemblyName(),
            new AssemblyName("System"),
            new AssemblyName("System.Does.Not.Exist"),
            new AssemblyName("Microsoft.Does.Not.Exist"),
            new AssemblyName("netstandard"),
            new AssemblyName("mscorlib"),
            new AssemblyName("WindowsBase"),
            new AssemblyName("Windows.Does.Not.Exist"));

        DispatcherDiscovery.FindCandidateAssemblies(root, out var skipped);

        Assert.Empty(skipped);
    }

    [Fact]
    public void FindCandidateAssemblies_ShouldNotTreatNamesThatMerelyStartWithFrameworkPrefixesAsFramework()
    {
        var root = CreateMockRoot(new AssemblyName("SystemMonitor.Handlers"), new AssemblyName("MicrosoftLike.Handlers"));

        DispatcherDiscovery.FindCandidateAssemblies(root, out var skipped);

        Assert.Equal(["SystemMonitor.Handlers", "MicrosoftLike.Handlers"], skipped);
    }

    [Fact]
    public void FindCandidateAssemblies_ShouldIgnoreReferencedAssembliesThatAreNotDispatcherAssemblies()
    {
        var root = CreateMockRoot(new AssemblyName("DevSource.Dispatcher"));

        var assemblies = DispatcherDiscovery.FindCandidateAssemblies(root, out var skipped);

        Assert.DoesNotContain(typeof(IMediator).Assembly, assemblies);
        Assert.Empty(skipped);
    }

    [Fact]
    public void FindCandidateAssemblies_ShouldIncludeReferencedDispatcherAssemblyOfMockedRoot()
    {
        var root = CreateMockRoot(FixtureAssembly.GetName());

        var assemblies = DispatcherDiscovery.FindCandidateAssemblies(root);

        Assert.Contains(FixtureAssembly, assemblies);
    }

    [Fact]
    public void FindCandidateAssemblies_WhenReferenceCannotBeLoaded_ShouldSkipAndReportIt()
    {
        var root = CreateMockRoot(new AssemblyName("Missing.Dispatcher.Dependency"));

        var assemblies = DispatcherDiscovery.FindCandidateAssemblies(root, out var skipped);

        Assert.Contains(root, assemblies);
        Assert.Equal(["Missing.Dispatcher.Dependency"], skipped);
    }

    [Fact]
    public void FindCandidateAssemblies_WhenReferenceIsNotAValidAssembly_ShouldSkipAndReportIt()
    {
        var path = Path.Combine(AppContext.BaseDirectory, $"BadImage{Guid.NewGuid():N}.dll");
        File.WriteAllText(path, "not an assembly");
        try
        {
            var root = CreateMockRoot(new AssemblyName(Path.GetFileNameWithoutExtension(path)));

            DispatcherDiscovery.FindCandidateAssemblies(root, out var skipped);

            Assert.Equal([Path.GetFileNameWithoutExtension(path)], skipped);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void FindGeneratedDispatcherType_ShouldReturnGeneratedTypeFromDispatcherAssembly()
    {
        var type = DispatcherDiscovery.FindGeneratedDispatcherType([typeof(object).Assembly, FixtureAssembly, TestAssembly]);

        Assert.Equal("DevSource.Dispatcher.Generated.GeneratedDispatcher", type?.FullName);
        Assert.Equal(TestAssembly, type!.Assembly);
    }

    [Fact]
    public void FindGeneratedDispatcherType_WithoutGeneratedType_ShouldReturnNull()
    {
        var type = DispatcherDiscovery.FindGeneratedDispatcherType([typeof(object).Assembly, FixtureAssembly]);

        Assert.Null(type);
    }

    [Fact]
    public void FindGeneratedDispatcherType_ShouldIgnoreTypeThatIsNotAGeneratedDispatcher()
    {
        var assembly = DefineDispatcherAssembly("DynamicNotGenerated", TypeAttributes.Public | TypeAttributes.Class, parent: null);

        var type = DispatcherDiscovery.FindGeneratedDispatcherType([assembly]);

        Assert.Null(type);
    }

    [Fact]
    public void FindGeneratedDispatcherType_ShouldIgnoreAbstractGeneratedDispatcher()
    {
        var assembly = DefineDispatcherAssembly(
            "DynamicAbstractGenerated",
            TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Abstract,
            typeof(AbstractGeneratedDispatcherBase));

        var type = DispatcherDiscovery.FindGeneratedDispatcherType([assembly]);

        Assert.Null(type);
    }

    [Fact]
    public void RegisterDiscoveredAssemblies_WithoutGeneratedDispatcher_ShouldRegisterRuntimeDispatcher()
    {
        var services = new ServiceCollection();
        DispatcherDiscoveryReport? report = null;

        DispatcherServiceCollectionExtensions.RegisterDiscoveredAssemblies(
            services,
            FixtureAssembly,
            [FixtureAssembly],
            ["Skipped.Assembly"],
            discoveryReport => report = discoveryReport);
        var provider = services.BuildServiceProvider();

        Assert.NotNull(report);
        Assert.Equal(FixtureAssembly.GetName().Name, report!.RootAssemblyName);
        Assert.Null(report.GeneratedDispatcherTypeName);
        Assert.Equal(1, report.RegisteredCommandHandlerCount);
        Assert.Equal(["Skipped.Assembly"], report.SkippedAssemblies);
        Assert.IsType<FixtureCommandHandler>(provider.GetRequiredService<DevSource.Dispatcher.Commands.ICommandHandler<FixtureCommand>>());
        Assert.IsType<DevSource.Dispatcher.Engine.Mediator>(provider.GetRequiredService<IMediator>());
    }


    [Fact]
    public void AddDispatcherDiscovery_WithNullCallback_ShouldThrow()
    {
        var action = () => new ServiceCollection().AddDispatcherDiscovery(null!);

        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void AddDispatcherDiscovery_WithNullServices_ShouldThrow()
    {
        var action = () => DispatcherServiceCollectionExtensions.AddDispatcherDiscovery(null!);

        Assert.Throws<ArgumentNullException>(action);
    }

    private static Assembly CreateMockRoot(params AssemblyName[] references)
    {
        var root = new Mock<Assembly>();
        root.Setup(static assembly => assembly.GetName()).Returns(new AssemblyName("MockRoot"));
        root.Setup(assembly => assembly.GetReferencedAssemblies()).Returns(references);
        root.Setup(static assembly => assembly.GetCustomAttributesData()).Returns([]);
        root.Setup(static assembly => assembly.DefinedTypes).Returns([]);
        return root.Object;
    }

    private static AssemblyBuilder DefineDispatcherAssembly(string name, TypeAttributes attributes, Type? parent)
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(name), AssemblyBuilderAccess.Run);
        assembly.SetCustomAttribute(new CustomAttributeBuilder(typeof(DispatcherAssemblyAttribute).GetConstructor(Type.EmptyTypes)!, []));
        assembly.DefineDynamicModule("main")
            .DefineType("DevSource.Dispatcher.Generated.GeneratedDispatcher", attributes, parent)
            .CreateType();
        return assembly;
    }
}
