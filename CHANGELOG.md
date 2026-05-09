# Changelog

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
