using DevSource.Dispatcher.Commands;
using DevSource.Dispatcher.Engine;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace DevSource.Dispatcher.SourceGenerator.Tests;

public sealed class DispatcherSourceGeneratorTests
{
    private const string Contracts = """
        using System.Threading;
        using System.Threading.Tasks;
        using DevSource.Dispatcher.Commands;
        using DevSource.Dispatcher.Notifications;
        using DevSource.Dispatcher.Queries;

        namespace Sample;

        public sealed record PlainCommand : ICommand;
        public sealed record ResponseCommand : ICommand<int>;
        public sealed record SampleQuery : IQuery<string>;
        public sealed record SampleNotification : INotification;
        public sealed record Wrapper<T>(T Value) : ICommand;
        """;

    [Fact]
    public void Generate_ClassHandlers_EmitsAllRequestKinds()
    {
        var generated = Run($$"""
            {{Contracts}}

            public sealed class PlainHandler : ICommandHandler<PlainCommand>
            {
                public ValueTask HandleAsync(PlainCommand command, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
            }

            public sealed class ResponseHandler : ICommandHandler<ResponseCommand, int>
            {
                public ValueTask<int> HandleAsync(ResponseCommand command, CancellationToken cancellationToken = default) => ValueTask.FromResult(1);
            }

            public sealed class QueryHandler : IQueryHandler<SampleQuery, string>
            {
                public ValueTask<string> HandleAsync(SampleQuery query, CancellationToken cancellationToken = default) => ValueTask.FromResult("");
            }

            public sealed class NotificationHandler : INotificationHandler<SampleNotification>
            {
                public ValueTask HandleAsync(SampleNotification notification, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
            }
            """);

        Assert.Contains("command is global::Sample.PlainCommand", generated);
        Assert.Contains("command is global::Sample.ResponseCommand", generated);
        Assert.Contains("query is global::Sample.SampleQuery", generated);
        Assert.Contains("notification is global::Sample.SampleNotification", generated);
    }

    [Fact]
    public void Generate_RecordHandler_IsDiscovered()
    {
        var generated = Run($$"""
            {{Contracts}}

            public sealed record PlainRecordHandler : ICommandHandler<PlainCommand>
            {
                public ValueTask HandleAsync(PlainCommand command, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
            }
            """);

        Assert.Contains("command is global::Sample.PlainCommand", generated);
        Assert.Contains("DispatcherAssemblyAttribute", generated);
    }

    [Fact]
    public void Generate_OpenGenericHandler_IsSkippedAndOutputCompiles()
    {
        var generated = Run($$"""
            {{Contracts}}

            public sealed class GenericHandler<T> : ICommandHandler<Wrapper<T>>
            {
                public ValueTask HandleAsync(Wrapper<T> command, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
            }
            """);

        Assert.DoesNotContain("Wrapper", generated);
        Assert.DoesNotContain("DispatcherAssemblyAttribute", generated);
    }

    [Fact]
    public void Generate_HandlerNestedInGenericType_IsSkippedAndOutputCompiles()
    {
        var generated = Run($$"""
            {{Contracts}}

            public static class Outer<T>
            {
                public sealed class NestedHandler : ICommandHandler<PlainCommand>
                {
                    public ValueTask HandleAsync(PlainCommand command, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
                }
            }
            """);

        Assert.DoesNotContain("PlainCommand", generated);
    }

    [Fact]
    public void Generate_DuplicateHandlersForSameRequest_EmitsSingleBranch()
    {
        var generated = Run($$"""
            {{Contracts}}

            public sealed class FirstHandler : ICommandHandler<PlainCommand>
            {
                public ValueTask HandleAsync(PlainCommand command, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
            }

            public sealed class SecondHandler : ICommandHandler<PlainCommand>
            {
                public ValueTask HandleAsync(PlainCommand command, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
            }

            public sealed class FirstResponseHandler : ICommandHandler<ResponseCommand, int>
            {
                public ValueTask<int> HandleAsync(ResponseCommand command, CancellationToken cancellationToken = default) => ValueTask.FromResult(1);
            }

            public sealed class SecondResponseHandler : ICommandHandler<ResponseCommand, int>
            {
                public ValueTask<int> HandleAsync(ResponseCommand command, CancellationToken cancellationToken = default) => ValueTask.FromResult(2);
            }
            """);

        Assert.Single(Occurrences(generated, "command is global::Sample.PlainCommand"));
        Assert.Single(Occurrences(generated, "command is global::Sample.ResponseCommand"));
    }

    [Fact]
    public void Generate_PrivateNestedRequest_OutputCompiles()
    {
        Run($$"""
            {{Contracts}}

            public static class Host
            {
                private sealed record HiddenCommand : ICommand;

                private sealed class HiddenHandler : ICommandHandler<HiddenCommand>
                {
                    public ValueTask HandleAsync(HiddenCommand command, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
                }
            }
            """);
    }

    [Fact]
    public void Generate_WithoutDispatcherContractsReferenced_EmitsEmptyDispatcher()
    {
        var references = GetReferences()
            .Where(static reference => reference.Display is null || !reference.Display.Contains("DevSource.Dispatcher", StringComparison.Ordinal))
            .ToList();
        var compilation = CSharpCompilation.Create(
            "NoContracts",
            [CSharpSyntaxTree.ParseText("namespace Sample; public sealed class Plain : System.IEquatable<Plain> { public bool Equals(Plain? other) => true; }", cancellationToken: TestContext.Current.CancellationToken)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var driver = CSharpGeneratorDriver.Create(new DispatcherSourceGenerator())
            .RunGenerators(compilation, TestContext.Current.CancellationToken);

        var generated = driver.GetRunResult().GeneratedTrees.Single().ToString();
        Assert.DoesNotContain("DispatcherAssemblyAttribute", generated);
        Assert.DoesNotContain(" is global::", generated);
    }

    private static IEnumerable<int> Occurrences(string text, string value)
    {
        for (var index = text.IndexOf(value, StringComparison.Ordinal); index >= 0; index = text.IndexOf(value, index + 1, StringComparison.Ordinal))
            yield return index;
    }

    private static string Run(string source)
    {
        var compilation = CSharpCompilation.Create(
            "GeneratorInput",
            [CSharpSyntaxTree.ParseText(source)],
            GetReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        var driver = CSharpGeneratorDriver.Create(new DispatcherSourceGenerator())
            .RunGeneratorsAndUpdateCompilation(compilation, out var updated, out var generatorDiagnostics, TestContext.Current.CancellationToken);

        Assert.Empty(generatorDiagnostics);
        Assert.Empty(updated.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error));

        return driver.GetRunResult().GeneratedTrees.Single().ToString();
    }

    private static List<MetadataReference> GetReferences()
    {
        var paths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Append(typeof(ICommand).Assembly.Location)
            .Append(typeof(GeneratedDispatchExecutor).Assembly.Location)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        return paths.Select(static path => (MetadataReference)MetadataReference.CreateFromFile(path)).ToList();
    }
}
