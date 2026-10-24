using DevSource.Dispatcher.Behaviors;
using DevSource.Dispatcher.Commands;
using DevSource.Dispatcher.Engine;
using DevSource.Dispatcher.Queries;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace DevSource.Dispatcher.Tests;

public sealed class BuiltInBehaviorsTests
{
    private static readonly ValidationError NameRequired = new("Name", "Name is required.");
    private static readonly ValidationError AgeInvalid = new("Age", "Age is invalid.");

    [Fact]
    public async Task LoggingBehavior_OnSuccess_ShouldLogStartAndDurationWithoutTheRequest()
    {
        var logger = new CapturingLogger<LoggingBehavior<TestQuery, int>>();
        var behavior = new LoggingBehavior<TestQuery, int>(logger);

        var response = await behavior.HandleAsync(new TestQuery(7, new TrackingState()), static () => ValueTask.FromResult(14), CancellationToken.None);

        Assert.Equal(14, response);
        Assert.Collection(
            logger.Entries,
            entry => Assert.Equal((LogLevel.Information, "Handling TestQuery"), (entry.Level, entry.Message)),
            entry =>
            {
                Assert.Equal(LogLevel.Information, entry.Level);
                Assert.StartsWith("Handled TestQuery in ", entry.Message, StringComparison.Ordinal);
                Assert.DoesNotContain("TrackingState", entry.Message, StringComparison.Ordinal);
            });
    }

    [Fact]
    public async Task LoggingBehavior_OnFailure_ShouldLogErrorAndRethrowSameException()
    {
        var logger = new CapturingLogger<LoggingBehavior<TestQuery, int>>();
        var behavior = new LoggingBehavior<TestQuery, int>(logger);
        var failure = new InvalidOperationException("boom");

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await behavior.HandleAsync(new TestQuery(1, new TrackingState()), () => throw failure, CancellationToken.None));

        Assert.Same(failure, thrown);
        var entry = Assert.Single(logger.Entries, static entry => entry.Level == LogLevel.Error);
        Assert.Same(failure, entry.Exception);
        Assert.StartsWith("TestQuery failed after ", entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoggingCommandBehavior_OnSuccess_ShouldLogStartAndDuration()
    {
        var logger = new CapturingLogger<LoggingCommandBehavior<TestCommand>>();
        var behavior = new LoggingCommandBehavior<TestCommand>(logger);
        var ran = false;

        await behavior.HandleAsync(new TestCommand(new TrackingState()), () =>
        {
            ran = true;
            return ValueTask.CompletedTask;
        }, CancellationToken.None);

        Assert.True(ran);
        Assert.Equal(2, logger.Entries.Count);
        Assert.Equal("Handling TestCommand", logger.Entries[0].Message);
        Assert.StartsWith("Handled TestCommand in ", logger.Entries[1].Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoggingCommandBehavior_OnFailure_ShouldLogErrorAndRethrowSameException()
    {
        var logger = new CapturingLogger<LoggingCommandBehavior<TestCommand>>();
        var behavior = new LoggingCommandBehavior<TestCommand>(logger);
        var failure = new InvalidOperationException("boom");

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await behavior.HandleAsync(new TestCommand(new TrackingState()), () => throw failure, CancellationToken.None));

        Assert.Same(failure, thrown);
        Assert.Same(failure, Assert.Single(logger.Entries, static entry => entry.Level == LogLevel.Error).Exception);
    }

    [Fact]
    public async Task ValidationBehavior_WithoutValidators_ShouldCallNext()
    {
        var behavior = new ValidationBehavior<TestQuery, int>([]);

        var response = await behavior.HandleAsync(new TestQuery(3, new TrackingState()), static () => ValueTask.FromResult(6), CancellationToken.None);

        Assert.Equal(6, response);
    }

    [Fact]
    public async Task ValidationBehavior_WithPassingValidators_ShouldCallNextAndPassTheToken()
    {
        using var source = new CancellationTokenSource();
        var validator = new StubValidator<TestQuery>();
        var second = new StubValidator<TestQuery>();
        var behavior = new ValidationBehavior<TestQuery, int>([validator, second]);

        var response = await behavior.HandleAsync(new TestQuery(3, new TrackingState()), static () => ValueTask.FromResult(6), source.Token);

        Assert.Equal(6, response);
        Assert.Equal(source.Token, validator.LastToken);
        Assert.Equal(source.Token, second.LastToken);
    }

    [Fact]
    public async Task ValidationBehavior_WithFailingValidators_ShouldThrowWithAllErrorsAndSkipNext()
    {
        var behavior = new ValidationBehavior<TestQuery, int>([
            new StubValidator<TestQuery>(NameRequired),
            new StubValidator<TestQuery>(),
            new StubValidator<TestQuery>(AgeInvalid)]);
        var nextCalled = false;

        var exception = await Assert.ThrowsAsync<RequestValidationException>(async () =>
            await behavior.HandleAsync(new TestQuery(3, new TrackingState()), () =>
            {
                nextCalled = true;
                return ValueTask.FromResult(6);
            }, CancellationToken.None));

        Assert.False(nextCalled);
        Assert.Equal([NameRequired, AgeInvalid], exception.Errors);
        Assert.Contains("2 error(s)", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ValidationCommandBehavior_ShouldCallNextOnlyWhenValid()
    {
        var calls = 0;
        var valid = new ValidationCommandBehavior<TestCommand>([new StubValidator<TestCommand>()]);
        var invalid = new ValidationCommandBehavior<TestCommand>([new StubValidator<TestCommand>(NameRequired)]);
        var withoutValidators = new ValidationCommandBehavior<TestCommand>([]);

        await valid.HandleAsync(new TestCommand(new TrackingState()), () => { calls++; return ValueTask.CompletedTask; }, CancellationToken.None);
        await withoutValidators.HandleAsync(new TestCommand(new TrackingState()), () => { calls++; return ValueTask.CompletedTask; }, CancellationToken.None);
        var exception = await Assert.ThrowsAsync<RequestValidationException>(async () =>
            await invalid.HandleAsync(new TestCommand(new TrackingState()), () => { calls++; return ValueTask.CompletedTask; }, CancellationToken.None));

        Assert.Equal(2, calls);
        Assert.Equal([NameRequired], exception.Errors);
    }

    [Fact]
    public void Behaviors_ShouldRunLoggingBeforeValidation()
    {
        Assert.True(new LoggingBehavior<TestQuery, int>(new CapturingLogger<LoggingBehavior<TestQuery, int>>()).Order
            < new ValidationBehavior<TestQuery, int>([]).Order);
        Assert.True(new LoggingCommandBehavior<TestCommand>(new CapturingLogger<LoggingCommandBehavior<TestCommand>>()).Order
            < new ValidationCommandBehavior<TestCommand>([]).Order);
    }

    [Fact]
    public async Task Pipeline_WithBothBehaviors_ShouldLogValidationFailures()
    {
        var logger = new CapturingLogger<LoggingCommandBehavior<TestCommand>>();
        var services = new ServiceCollection();
        services.AddSingleton<ILogger<LoggingCommandBehavior<TestCommand>>>(logger);
        services.AddTransient<ICommandHandler<TestCommand>, TestCommandHandler>();
        services.AddTransient<IRequestValidator<TestCommand>>(_ => new StubValidator<TestCommand>(NameRequired));
        services.AddDispatcher();
        services.AddDispatcherValidation();
        services.AddDispatcherLogging();
        var tracking = new TrackingState();

        await Assert.ThrowsAsync<RequestValidationException>(async () =>
            await services.BuildServiceProvider().GetRequiredService<IMediator>().SendAsync(new TestCommand(tracking), TestContext.Current.CancellationToken));

        Assert.Empty(tracking.Entries);
        Assert.Contains(logger.Entries, static entry => entry.Level == LogLevel.Error && entry.Exception is RequestValidationException);
    }

    [Fact]
    public async Task Pipeline_WithResponseRequests_ShouldWrapQueriesWithBothBehaviors()
    {
        var logger = new CapturingLogger<LoggingBehavior<TestQuery, int>>();
        var services = new ServiceCollection();
        services.AddSingleton<ILogger<LoggingBehavior<TestQuery, int>>>(logger);
        services.AddTransient<IQueryHandler<TestQuery, int>, TestQueryHandler>();
        services.AddDispatcher();
        services.AddDispatcherLogging();
        services.AddDispatcherValidation();

        var response = await services.BuildServiceProvider().GetRequiredService<IMediator>().QueryAsync<TestQuery, int>(new TestQuery(4, new TrackingState()), TestContext.Current.CancellationToken);

        Assert.Equal(8, response);
        Assert.Equal(2, logger.Entries.Count);
    }

    [Fact]
    public async Task LoggingBehavior_ShouldNotPassTheRequestAsALogArgument()
    {
        var logger = new CapturingLogger<LoggingBehavior<TestQuery, int>>();
        var request = new TestQuery(7, new TrackingState());

        await new LoggingBehavior<TestQuery, int>(logger).HandleAsync(request, static () => ValueTask.FromResult(1), CancellationToken.None);

        Assert.All(logger.Entries, entry => Assert.DoesNotContain(request, entry.Arguments));
    }

    [Fact]
    public async Task Pipeline_WithFailingValidatorOnQuery_ShouldNotReachTheHandler()
    {
        var logger = new CapturingLogger<LoggingBehavior<TestQuery, int>>();
        var tracking = new TrackingState();
        var services = new ServiceCollection();
        services.AddSingleton<ILogger<LoggingBehavior<TestQuery, int>>>(logger);
        services.AddTransient<IQueryHandler<TestQuery, int>, TestQueryHandler>();
        services.AddTransient<IRequestValidator<TestQuery>>(_ => new StubValidator<TestQuery>(AgeInvalid));
        services.AddDispatcher();
        services.AddDispatcherValidation();
        services.AddDispatcherLogging();

        var exception = await Assert.ThrowsAsync<RequestValidationException>(async () =>
            await services.BuildServiceProvider().GetRequiredService<IMediator>().QueryAsync<TestQuery, int>(new TestQuery(4, tracking), TestContext.Current.CancellationToken));

        Assert.Equal([AgeInvalid], exception.Errors);
        Assert.Empty(tracking.Entries);
        Assert.Contains(logger.Entries, static entry => entry.Level == LogLevel.Error && entry.Exception is RequestValidationException);
    }

    [Fact]
    public void AddDispatcherLoggingAndValidation_CalledTwice_ShouldNotDuplicateRegistrations()
    {
        var services = new ServiceCollection();

        services.AddDispatcherLogging().AddDispatcherLogging().AddDispatcherValidation().AddDispatcherValidation();

        Assert.Equal(
            [typeof(LoggingBehavior<,>), typeof(LoggingCommandBehavior<>), typeof(ValidationBehavior<,>), typeof(ValidationCommandBehavior<>)],
            services.Select(static descriptor => descriptor.ImplementationType!).ToArray());
    }

    [Fact]
    public void AddDispatcherLoggingAndValidation_WithNullServices_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => BehaviorsServiceCollectionExtensions.AddDispatcherLogging(null!));
        Assert.Throws<ArgumentNullException>(() => BehaviorsServiceCollectionExtensions.AddDispatcherValidation(null!));
    }

    private sealed class StubValidator<TRequest>(params ValidationError[] errors) : IRequestValidator<TRequest>
    {
        public CancellationToken LastToken { get; private set; }

        public ValueTask<IReadOnlyList<ValidationError>> ValidateAsync(TRequest request, CancellationToken cancellationToken)
        {
            LastToken = cancellationToken;
            return ValueTask.FromResult<IReadOnlyList<ValidationError>>(errors);
        }
    }

    private sealed record LogEntry(LogLevel Level, string Message, Exception? Exception, object?[] Arguments);

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        private readonly List<LogEntry> _entries = [];

        public IReadOnlyList<LogEntry> Entries => _entries;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => _entries.Add(new LogEntry(
                logLevel,
                formatter(state, exception),
                exception,
                (state as IEnumerable<KeyValuePair<string, object?>>)?.Select(static pair => pair.Value).ToArray() ?? []));
    }
}
