using BenchmarkDotNet.Running;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wolverine;
using Wolverine.Transports;

namespace DevSource.Dispatcher.Benchmarks;

internal static class Program
{
    public static int Main(string[] args)
    {
        if (args.Contains("--diagnose-wolverine", StringComparer.Ordinal))
        {
            DiagnoseWolverineAsync().GetAwaiter().GetResult();
            return 0;
        }

        BenchmarkRunner.Run<DispatcherBenchmarks>();
        return 0;
    }

    private static async Task DiagnoseWolverineAsync()
    {
        using var host = await Host.CreateDefaultBuilder()
            .UseWolverine(options =>
            {
                options.ApplicationAssembly = typeof(DispatcherBenchmarks).Assembly;
                options.Durability.Mode = DurabilityMode.MediatorOnly;
                options.Discovery.IncludeAssembly(typeof(DispatcherBenchmarks).Assembly);
                options.Discovery.IncludeType<WolverineCommandHandler>();
                options.Discovery.IncludeType<WolverineQueryHandler>();
                options.PublishMessage<WolverineCommand>().ToLocalQueue("wolverine-command");
                options.PublishMessage<WolverineQuery>().ToLocalQueue("wolverine-query");
                Console.WriteLine(options.DescribeHandlerMatch(typeof(WolverineCommandHandler)));
                Console.WriteLine(options.DescribeHandlerMatch(typeof(WolverineQueryHandler)));
            })
            .StartAsync()
            .ConfigureAwait(false);

        try
        {
            WolverineBenchmarkProbe.ResetCommand();
            await host.Services.GetRequiredService<IMessageBus>().InvokeAsync(new WolverineCommand(42)).ConfigureAwait(false);
            Console.WriteLine($"WolverineCommand result: {await WolverineBenchmarkProbe.CommandTask.ConfigureAwait(false)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WolverineCommand error: {ex}");
        }

        try
        {
            WolverineBenchmarkProbe.ResetQuery();
            await host.Services.GetRequiredService<IMessageBus>().InvokeAsync(new WolverineQuery(42)).ConfigureAwait(false);
            Console.WriteLine($"WolverineQuery result: {await WolverineBenchmarkProbe.QueryTask.ConfigureAwait(false)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WolverineQuery error: {ex}");
        }
    }
}
