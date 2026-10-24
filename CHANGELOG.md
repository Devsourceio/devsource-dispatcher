# Changelog

## 1.2.0 - 2026-10-24

- Added `AddDispatcherBehavior<TBehavior>()` and `AddDispatcherBehavior(Type)` to register closed and open generic pipeline behaviors.
- The package now also depends on `Microsoft.Extensions.Logging.Abstractions`.
- Updated the package repository URL to `https://github.com/Devsourceio/devsource-dispatcher`.
- Added ready-made behaviors in the `DevSource.Dispatcher` package (namespace `DevSource.Dispatcher.Behaviors`): `LoggingBehavior` and `ValidationBehavior` (plus command-only variants) and `IRequestValidator<T>`, registered through `AddDispatcherLogging()` and `AddDispatcherValidation()`.
- Fixed the source generator emitting uncompilable code for open generic handlers, handlers nested in generic types, and requests that are not accessible from generated code.
- Fixed the source generator ignoring `record` handlers.
- Discovery no longer treats names that merely start with `System`, `Microsoft`, or `Windows` as framework assemblies.
- Discovery now accepts every loaded assembly marked as a dispatcher assembly, even when the root assembly's metadata does not reference it.
- Discovery skips referenced assemblies that cannot be loaded instead of failing, and lists them in `DispatcherDiscoveryReport.SkippedAssemblies`.
- Generated dispatchers are no longer `async`: handled requests return the handler task through new `GeneratedDispatchExecutor` helpers (`ExecuteCommandHandledAsync`, `ExecuteQueryHandledAsync`, `PublishNotificationHandledAsync`). Throughput on the benchmark is unchanged.
- Reduced dispatch overhead: dispatchers and `Mediator` no longer wrap calls in `async` state machines, and requests without pipeline behaviors skip behavior ordering. In the benchmark project the runtime command path dropped from about 82 ns / 280 B to about 45 ns / 48 B.
- Behavior change: `ArgumentNullException` for a null request and `InvalidOperationException` for a missing handler are now thrown synchronously from `DispatchAsync`, `SendAsync`, `QueryAsync` and `PublishAsync` instead of surfacing from the awaited `ValueTask`. The same applies to non-`async` handlers and behaviors that throw before returning a `ValueTask`. Code that awaits the call directly is unaffected.

## 1.1.0

- Added `AddDispatcherDiscovery()` for one-call dispatcher bootstrap with automatic handler discovery.
- Added automatic assembly scanning and DI registration for command, query, and notification handlers.
- Added automatic generated-dispatcher wiring when `DevSource.Dispatcher.Generated.GeneratedDispatcher` is available.
- Hardened discovery to trust only the calling assembly plus explicitly marked dispatcher assemblies.
- Hardened generated dispatcher selection to reject ambiguous discovery results.
- Added `DispatcherDiscoveryReport` and discovery callback support for diagnostics and telemetry.
- Added explicit assembly scan overloads for discovery-based registration scenarios.
- Added and updated tests covering discovery, generated dispatcher wiring, and DI registration behavior.
- Added tests for discovery diagnostics, duplicate handler conflicts, and multiple generated dispatcher handling.
- Reworked `samples/` to demonstrate layered usage with `AddDispatcherDiscovery()` and local package consumption.
- Updated `README.md` to document discovery-based registration and the new sample structure.
