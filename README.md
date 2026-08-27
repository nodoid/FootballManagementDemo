# Football Management Engine — .NET MAUI Demo

This solution turns the supplied `FootballManagementEngine` into a reusable `net10.0` class library and adds a simple .NET MAUI client.

## Targets

The MAUI project declares all four major .NET MAUI desktop/mobile targets:

- Android
- iOS
- Mac Catalyst
- Windows

The UI uses only .NET MAUI controls and the supplied library, so the application logic is shared across platforms.

## What the demo shows

1. Creates the supplied `UkDatabase`.
2. Generates domestic, FA Cup and sample European fixtures.
3. Calls `GameApi.Handle("GET", "/api/teams")` to populate the club picker.
4. Calls `POST /api/game/select-team` when a club is selected.
5. Calls `GET /api/game` and displays the JSON response.
6. Uses `SeasonEngine.ProcessWeek()` to advance time and process weekly finance/injuries.
7. Uses `MatchSimulator` and `FootballGameEngine.ApplyResult()` to simulate the next match.

No HTTP server is required: this demonstrates the library's server-agnostic application API being consumed directly by a cross-platform client.

## Build / run

Install the .NET 10 SDK and the required .NET MAUI workloads on your development machine.

From this directory:

```bash
dotnet restore FootballManagementDemo.sln
```

Then run a target appropriate to your machine, for example:

```bash
dotnet build FootballManagementDemo/FootballManagementDemo.csproj -f net10.0-android
dotnet build FootballManagementDemo/FootballManagementDemo.csproj -f net10.0-windows10.0.19041.0
dotnet build FootballManagementDemo/FootballManagementDemo.csproj -f net10.0-maccatalyst
dotnet build FootballManagementDemo/FootballManagementDemo.csproj -f net10.0-ios
```

For an iOS or Mac Catalyst deployment you will need the normal Apple/Xcode signing environment. Android needs an Android SDK/emulator or device; Windows needs the Windows App SDK environment supported by your installed MAUI workload.

## Architecture

```text
FootballManagementDemo (MAUI)
        |
        | direct C# calls
        v
FootballManagementEngine (net10.0)
        |
        +-- GameApi
        +-- FootballGameEngine
        +-- SeasonEngine
        +-- MatchSimulator
        +-- UkDatabase
```

The original library remains under `FootballManagementEngine/`, with its project changed from an executable to a reusable class library.

## Notes

The football data in the supplied library is illustrative/generated data, not licensed current squad data.

## License

This application (as with the library it is based off) is covered by the DILLIGAF license. Do with it what you will
