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

After installing the package, you can use the library in two ways:

- Runtime mode: register handlers and behaviors yourself, then call `AddDispatcher()`
- Generated mode: let the source generator emit both `GeneratedDispatcher` and DI registration code, then call `AddGeneratedDispatcher()`

## Quick Start

### Mode 1: Runtime registration

Use this mode when you want explicit control over DI registration, or when you do not want to depend on generated registration code.

```csharp
using DevSource.Dispatcher.Engine;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddOrderApplication(this IServiceCollection services)
    {
        services.AddTransient<ICommandHandler<CreateOrderCommand, Guid>, CreateOrderHandler>();
        services.AddTransient<IQueryHandler<GetOrderQuery, OrderDto>, GetOrderHandler>();
        services.AddTransient<INotificationHandler<OrderCreatedNotification>, OrderCreatedHandler>();

        services.AddDispatcher();
    }
}
```

In this mode:

- handler and behavior registration is explicit
- the dispatcher runtime is registered by `AddDispatcher()`
- execution still uses the runtime engine with cached delegates and deterministic pipeline ordering

### Mode 2: Generated registration

Use this mode when you want the library to register dispatcher handlers and behaviors automatically without reflection.

When analyzer support is active, the package emits:

- `DevSource.Dispatcher.Generated.GeneratedDispatcher`
- `DevSource.Dispatcher.Generated.GeneratedServiceCollectionExtensions`

Then you can register everything discovered at compile-time with a single call:

```csharp
using DevSource.Dispatcher.Engine;
using DevSource.Dispatcher.Generated;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddOrderApplication(this IServiceCollection services)
    {
        return services.AddGeneratedDispatcher();
    }
}
```

In this mode, the generated code registers handlers, notifications, pipeline behaviors, and the generated-first dispatcher path without runtime reflection.

Important:

- only dispatcher-related types discovered in the current compilation are auto-registered
- external infrastructure dependencies still need explicit registration by the application
- examples: repositories, clocks, database connections, HTTP clients, logging sinks

Example:

```csharp
using DevSource.Dispatcher.Generated;
using Microsoft.Extensions.DependencyInjection;
using Order.Application;
using Order.Application.Abstractions;
using Order.Domain;

var services = new ServiceCollection();

services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();
services.AddSingleton<IClock, SystemClock>();

services.AddGeneratedDispatcher();
```

## Layered Sample

The repository includes a real layered sample under `samples/`:

- `samples/Order.Domain` - domain model and repository abstraction
- `samples/Order.Application` - commands, queries, notifications, handlers, pipeline behavior, and generated DI registration usage
- `samples/Order.Api` - Minimal API host showing how to wire everything together

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
dotnet build samples/Order.Api/Order.Api.csproj
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
