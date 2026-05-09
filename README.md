# DevSource.Dispatcher

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Tests](https://img.shields.io/badge/tests-passing-brightgreen)](#running-tests)
[![BenchmarkDotNet](https://img.shields.io/badge/benchmarks-BenchmarkDotNet-blue)](https://benchmarkdotnet.org/)
[![CQRS](https://img.shields.io/badge/pattern-CQRS-orange)](#overview)

DevSource.Dispatcher is a high-performance .NET dispatcher built as an open-source alternative to MediatR.

It focuses on:

- Decoupled use-case execution with commands, queries, and notifications
- Deterministic and extensible pipelines for cross-cutting concerns
- Minimal runtime overhead with `ValueTask`, delegate caching, and generated dispatch
- Runtime independence from frameworks and mandatory DI containers
- A hybrid execution model: generated first, runtime fallback second

## Overview

The solution is organized into three main projects:

- `src/DevSource.Dispatcher` - public contracts only, with no external dependencies
- `src/DevSource.Dispatcher.Engine` - runtime engine, pipeline orchestration, caching, and DI integration helpers
- `src/DevSource.Dispatcher.SourceGenerator` - compile-time dispatcher generation

Execution flow:

`Request -> Dispatcher -> Pipeline -> Handler -> Response`

Hybrid strategy:

1. Try generated dispatch code
2. Fallback to runtime resolution

## Features

- Commands with and without responses
- Queries with single-handler execution
- Notifications with fan-out publishing
- Optional pipeline behaviors with deterministic ordering
- `CancellationToken` support
- `ValueTask`-based APIs
- Delegate caching per request type
- Source-generator integration for generated dispatch paths
- Works with or without direct `IServiceProvider` usage

## Installation

The recommended installation experience is a single package:

```bash
dotnet add package DevSource.Dispatcher
```

The `DevSource.Dispatcher` package is the public entry point and is intended to deliver the full experience in one install:

- public contracts
- runtime engine
- source generator analyzer

That means consumers do not need to install `DevSource.Dispatcher.Engine` or `DevSource.Dispatcher.SourceGenerator` separately when using the NuGet package.

After installing the package, you can register the dispatcher with a single discovery call:

```csharp
using DevSource.Dispatcher.Engine;
using DevSource.Dispatcher.Generated;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddDispatcherDiscovery();
```

## Quick Start

### Register the runtime engine with discovery

```csharp
using DevSource.Dispatcher.Engine;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddDispatcherDiscovery();
```

When the package is installed with analyzer support enabled, a `GeneratedDispatcher` type is emitted in `DevSource.Dispatcher.Generated` and `AddDispatcherDiscovery()` wires it automatically when found.

```csharp
using DevSource.Dispatcher.Engine;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddDispatcherDiscovery();
```

### Keep explicit registrations if you prefer

If you want full control over DI registrations, the explicit style remains supported:

```csharp
using DevSource.Dispatcher.Engine;
using DevSource.Dispatcher.Generated;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddTransient<ICommandHandler<CreateOrderCommand, Guid>, CreateOrderHandler>();
services.AddTransient<IQueryHandler<GetOrderQuery, OrderDto>, GetOrderHandler>();
services.AddTransient<INotificationHandler<OrderCreatedNotification>, OrderCreatedHandler>();

services.AddDispatcher<GeneratedDispatcher>();
```

### Register a specific assembly explicitly

If you prefer explicit assembly targeting instead of automatic discovery, the scan-based overloads remain available:

```csharp
services.AddDispatcherFromAssemblyContaining<CreateOrderHandler>();
services.AddDispatcherFromAssemblyContaining<CreateOrderHandler, GeneratedDispatcher>();
```

### Use without DI

You can also use the dispatcher without `IServiceProvider`. In this mode, you provide your own resolver.

```csharp
using DevSource.Dispatcher.Commands;
using DevSource.Dispatcher.Engine;
using DevSource.Dispatcher.Notifications;
using DevSource.Dispatcher.Queries;

var resolver = new ManualResolver(
    new CreateOrderHandler(new InMemoryOrderRepository()),
    new GetOrderHandler(new InMemoryOrderRepository()));

var commandDispatcher = new CommandDispatcher(resolver);
var queryDispatcher = new QueryDispatcher(resolver);
var notificationDispatcher = new NotificationDispatcher(resolver);
var mediator = new DevSource.Dispatcher.Engine.Mediator(commandDispatcher, queryDispatcher, notificationDispatcher);

var orderId = await mediator.SendAsync<CreateOrderCommand, Guid>(new CreateOrderCommand("Ada Lovelace"));

sealed class ManualResolver : IRequestHandlerResolver
{
    private readonly ICommandHandler<CreateOrderCommand, Guid> _commandHandler;
    private readonly IQueryHandler<GetOrderQuery, OrderDto> _queryHandler;

    public ManualResolver(
        ICommandHandler<CreateOrderCommand, Guid> commandHandler,
        IQueryHandler<GetOrderQuery, OrderDto> queryHandler)
    {
        _commandHandler = commandHandler;
        _queryHandler = queryHandler;
    }

    public ICommandHandler<TCommand> GetRequiredCommandHandler<TCommand>() where TCommand : ICommand
        => throw new NotSupportedException();

    public ICommandHandler<TCommand, TResponse> GetRequiredCommandHandler<TCommand, TResponse>() where TCommand : ICommand<TResponse>
        => typeof(TCommand) == typeof(CreateOrderCommand) && typeof(TResponse) == typeof(Guid)
            ? (ICommandHandler<TCommand, TResponse>)_commandHandler
            : throw new InvalidOperationException($"No handler for {typeof(TCommand).Name}.");

    public IQueryHandler<TQuery, TResponse> GetRequiredQueryHandler<TQuery, TResponse>() where TQuery : IQuery<TResponse>
        => typeof(TQuery) == typeof(GetOrderQuery) && typeof(TResponse) == typeof(OrderDto)
            ? (IQueryHandler<TQuery, TResponse>)_queryHandler
            : throw new InvalidOperationException($"No handler for {typeof(TQuery).Name}.");

    public IEnumerable<IPipelineBehavior<TCommand>> GetCommandBehaviors<TCommand>() where TCommand : ICommand => [];

    public IEnumerable<IPipelineBehavior<TRequest, TResponse>> GetBehaviors<TRequest, TResponse>() where TRequest : notnull => [];

    public IEnumerable<INotificationHandler<TNotification>> GetNotificationHandlers<TNotification>() where TNotification : INotification => [];
}
```

## Layered Sample

The repository includes a real layered sample under `samples/`:

- `samples/Order.Domain` - domain model and repository abstraction
- `samples/Order.Application` - commands, queries, DTOs, and handlers discovered automatically
- `samples/Order.Infrastructure` - repository implementation and infrastructure registrations
- `samples/Order.Api` - Minimal API host using only `services.AddDispatcherDiscovery()`

The sample intentionally consumes the library from a local NuGet package instead of `ProjectReference` entries to `src/`, so it stays close to a real consumer application.

Build the library artifacts first:

```bash
dotnet build src/DevSource.Dispatcher/DevSource.Dispatcher.csproj -c Release
```

That build writes the `.nupkg` used by `samples/NuGet.Config` into `src/DevSource.Dispatcher/bin/Release`.

Run the sample API with:

```bash
dotnet run --project samples/Order.Api/Order.Api.csproj
```

Example requests:

```bash
curl -X POST http://localhost:5000/orders \
  -H "Content-Type: application/json" \
  -d "{\"customerName\":\"Ada Lovelace\",\"items\":[{\"productName\":\"Keyboard\",\"quantity\":1,\"unitPrice\":120.00}]}"
```

```bash
curl http://localhost:5000/orders/{orderId}
```

## Usage Examples

### Command with response

```csharp
using DevSource.Dispatcher.Commands;

public sealed record CreateOrderCommand(string CustomerName) : ICommand<Guid>;

public sealed class CreateOrderHandler : ICommandHandler<CreateOrderCommand, Guid>
{
    public ValueTask<Guid> HandleAsync(CreateOrderCommand command, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(Guid.NewGuid());
}
```

Dispatch it through `IMediator`:

```csharp
using DevSource.Dispatcher;

var mediator = serviceProvider.GetRequiredService<IMediator>();
var orderId = await mediator.SendAsync<CreateOrderCommand, Guid>(new CreateOrderCommand("Ada Lovelace"));
```

### Query

```csharp
using DevSource.Dispatcher.Queries;

public sealed record GetOrderQuery(Guid OrderId) : IQuery<OrderDto>;

public sealed record OrderDto(Guid Id, string CustomerName);

public sealed class GetOrderHandler : IQueryHandler<GetOrderQuery, OrderDto>
{
    public ValueTask<OrderDto> HandleAsync(GetOrderQuery query, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(new OrderDto(query.OrderId, "Ada Lovelace"));
}
```

Dispatch it:

```csharp
var order = await mediator.QueryAsync<GetOrderQuery, OrderDto>(new GetOrderQuery(orderId));
```

### Notification

```csharp
using DevSource.Dispatcher.Notifications;

public sealed record OrderCreatedNotification(Guid OrderId) : INotification;

public sealed class OrderCreatedHandler : INotificationHandler<OrderCreatedNotification>
{
    public ValueTask HandleAsync(OrderCreatedNotification notification, CancellationToken cancellationToken = default)
        => ValueTask.CompletedTask;
}
```

Publish it:

```csharp
await mediator.PublishAsync(new OrderCreatedNotification(orderId));
```

### Pipeline behavior

```csharp
using DevSource.Dispatcher;
using DevSource.Dispatcher.Engine;

public sealed class LoggingBehavior : IPipelineBehavior<CreateOrderCommand, Guid>, IOrderedPipelineBehavior
{
    public int Order => 100;

    public async ValueTask<Guid> HandleAsync(
        CreateOrderCommand request,
        RequestHandlerDelegate<Guid> next,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Creating order for {request.CustomerName}");
        var response = await next().ConfigureAwait(false);
        Console.WriteLine($"Created order {response}");
        return response;
    }
}
```

The engine executes ordered behaviors first by `Order`, then by type name as a deterministic fallback.

## Running Tests

Run the unit test suite with:

```bash
dotnet test tests/DevSource.Dispatcher.Tests/DevSource.Dispatcher.Tests.csproj
```

To validate the sample application build as well:

```bash
dotnet build src/DevSource.Dispatcher/DevSource.Dispatcher.csproj -c Release
dotnet build samples/Order.slnx -c Release
```

The project currently uses:

- xUnit
- Moq
- Bogus

## Running Benchmarks

Run the benchmark project with:

```bash
dotnet run --project tests/DevSource.Dispatcher.Benchmarks/DevSource.Dispatcher.Benchmarks.csproj -- --filter *DispatcherBenchmarks*
```

The benchmark compares DevSource.Dispatcher against:

- MediatR
- WolverineFx

Artifacts are written to:

- `tests/DevSource.Dispatcher.Benchmarks/BenchmarkDotNet.Artifacts/results/`

Important note:

- current benchmark artifacts were collected from the `RELEASE`-based run stored in `tests/DevSource.Dispatcher.Benchmarks/BenchmarkDotNet.Artifacts/results/DevSource.Dispatcher.Benchmarks.DispatcherBenchmarks-report-github.md`

Latest benchmark snapshot:

| Method                      |      Mean | Allocated |
|-----------------------------|----------:|----------:|
| DevSource_Runtime_Command   |  87.66 ns |     280 B |
| DevSource_Generated_Command |  97.16 ns |     280 B |
| Wolverine_Command           | 190.59 ns |     656 B |
| MediatR_Command             | 211.37 ns |    1376 B |
| DevSource_Runtime_Query     |  85.94 ns |     280 B |
| DevSource_Generated_Query   | 100.80 ns |     280 B |
| Wolverine_Query             | 192.75 ns |     656 B |
| MediatR_Query               | 266.44 ns |    1440 B |

In the current published benchmark artifacts, DevSource.Dispatcher is faster and allocates less memory than both MediatR and WolverineFx for the measured command and query scenarios.

## Roadmap

Potential next extensions include:

- Validation extensions
- Logging extensions
- Resilience extensions
- Observability extensions
- Streaming support
- Additional integration adapters

## Contributing

Contributions, issues, and design discussions are welcome.

If you want to contribute:

1. Fork the repository
2. Create a feature branch
3. Add or update tests
4. Run the test suite and benchmarks when relevant
5. Open a pull request

## Contributors

Current contributors:

- [Uitan Maciel](https://github.com/uitanmaciel)

## Repository Structure

```text
src/
  DevSource.Dispatcher/
  DevSource.Dispatcher.Engine/
  DevSource.Dispatcher.SourceGenerator/
samples/
  Order.Api/
  Order.Application/
  Order.Domain/
tests/
  DevSource.Dispatcher.Tests/
  DevSource.Dispatcher.Benchmarks/
DevSource.slnx
```
