using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using DevSource.Dispatcher.Commands;
using DevSource.Dispatcher.Engine;
using DevSource.Dispatcher.Queries;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wolverine;
using Wolverine.Runtime;

namespace DevSource.Dispatcher.Benchmarks;

[MemoryDiagnoser]
[Config(typeof(DispatcherBenchmarkConfig))]
public class DispatcherBenchmarks
{
    private ICommandDispatcher _devSourceRuntimeCommandDispatcher = null!;
    private ICommandDispatcher _devSourceGeneratedCommandDispatcher = null!;
    private IQueryDispatcher _devSourceRuntimeQueryDispatcher = null!;
    private IQueryDispatcher _devSourceGeneratedQueryDispatcher = null!;
    private MediatR.IMediator _mediatR = null!;
    private IMessageBus _wolverineBus = null!;

    [GlobalSetup]
    public async Task SetupAsync()
    {
        var runtimeServices = new ServiceCollection();
        runtimeServices.AddTransient<ICommandHandler<DevSourceCommand, int>, DevSourceCommandHandler>();
        runtimeServices.AddTransient<IQueryHandler<DevSourceQuery, int>, DevSourceQueryHandler>();
        runtimeServices.AddDispatcher();
        var runtimeProvider = runtimeServices.BuildServiceProvider();

        var generatedServices = new ServiceCollection();
        generatedServices.AddTransient<ICommandHandler<DevSourceCommand, int>, DevSourceCommandHandler>();
        generatedServices.AddTransient<IQueryHandler<DevSourceQuery, int>, DevSourceQueryHandler>();
        generatedServices.AddDispatcher<BenchmarkGeneratedDispatcher>();
        var generatedProvider = generatedServices.BuildServiceProvider();

        var mediatRServices = new ServiceCollection();
        mediatRServices.AddMediatR(typeof(DispatcherBenchmarks).Assembly);
        var mediatRProvider = mediatRServices.BuildServiceProvider();

        var wolverineHost = await Host.CreateDefaultBuilder()
            .UseWolverine(options =>
            {
                options.ApplicationAssembly = typeof(DispatcherBenchmarks).Assembly;
                options.Durability.Mode = DurabilityMode.MediatorOnly;
                options.Discovery.IncludeAssembly(typeof(DispatcherBenchmarks).Assembly);
                options.Discovery.IncludeType<WolverineCommandProbeHandler>();
                options.Discovery.IncludeType<WolverineQueryProbeHandler>();
                options.PublishMessage<WolverineCommand>().ToLocalQueue("wolverine-command");
                options.PublishMessage<WolverineQuery>().ToLocalQueue("wolverine-query");
            })
            .StartAsync()
            .ConfigureAwait(false);

        _devSourceRuntimeCommandDispatcher = runtimeProvider.GetRequiredService<ICommandDispatcher>();
        _devSourceGeneratedCommandDispatcher = generatedProvider.GetRequiredService<ICommandDispatcher>();
        _devSourceRuntimeQueryDispatcher = runtimeProvider.GetRequiredService<IQueryDispatcher>();
        _devSourceGeneratedQueryDispatcher = generatedProvider.GetRequiredService<IQueryDispatcher>();
        _mediatR = mediatRProvider.GetRequiredService<MediatR.IMediator>();
        _wolverineBus = wolverineHost.Services.GetRequiredService<IMessageBus>();
    }

    [Benchmark(Baseline = true)]
    public ValueTask<int> DevSource_Runtime_Command()
        => _devSourceRuntimeCommandDispatcher.DispatchAsync<DevSourceCommand, int>(new DevSourceCommand(42));

    [Benchmark]
    public ValueTask<int> DevSource_Generated_Command()
        => _devSourceGeneratedCommandDispatcher.DispatchAsync<DevSourceCommand, int>(new DevSourceCommand(42));

    [Benchmark]
    public Task<int> MediatR_Command()
        => _mediatR.Send(new MediatRCommand(42));

    [Benchmark]
    public async Task<int> Wolverine_Command()
    {
        WolverineBenchmarkProbe.ResetCommand();
        await _wolverineBus.InvokeAsync(new WolverineCommand(42)).ConfigureAwait(false);
        return await WolverineBenchmarkProbe.CommandTask.ConfigureAwait(false);
    }

    [Benchmark]
    public ValueTask<int> DevSource_Runtime_Query()
        => _devSourceRuntimeQueryDispatcher.DispatchAsync<DevSourceQuery, int>(new DevSourceQuery(42));

    [Benchmark]
    public ValueTask<int> DevSource_Generated_Query()
        => _devSourceGeneratedQueryDispatcher.DispatchAsync<DevSourceQuery, int>(new DevSourceQuery(42));

    [Benchmark]
    public Task<int> MediatR_Query()
        => _mediatR.Send(new MediatRQuery(42));

    [Benchmark]
    public async Task<int> Wolverine_Query()
    {
        WolverineBenchmarkProbe.ResetQuery();
        await _wolverineBus.InvokeAsync(new WolverineQuery(42)).ConfigureAwait(false);
        return await WolverineBenchmarkProbe.QueryTask.ConfigureAwait(false);
    }
}

internal sealed class DispatcherBenchmarkConfig : ManualConfig
{
    public DispatcherBenchmarkConfig()
    {
        ArtifactsPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "BenchmarkDotNet.Artifacts");
        Options |= ConfigOptions.DisableOptimizationsValidator;
        AddJob(Job.Default
            .WithToolchain(InProcessEmitToolchain.Instance)
            .WithLaunchCount(1)
            .WithWarmupCount(3)
            .WithIterationCount(5));
    }
}
