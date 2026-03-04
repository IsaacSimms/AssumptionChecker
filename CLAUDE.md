# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

AssumptionChecker analyzes prompts for hidden assumptions before they reach an AI system and suggests improved alternatives. It has four client surfaces (VS Extension, WPF desktop app, CLI, and a shared Engine API) that all communicate via HTTP to a single ASP.NET Core backend.

## Build Commands

```bash
# Build entire solution
dotnet build

# Build in release mode (required before packaging VSIX)
dotnet build -c Release

# Run tests
dotnet test
dotnet test --verbosity detailed

# Run a single test project
dotnet test AssumptionChecker.Tests

# Run Engine locally (listens on http://localhost:5046)
cd AssumptionChecker.Engine && dotnet run

# Run WPF app (auto-launches Engine)
cd AssumptionChecker.WPFApp && dotnet run

# Run CLI (Engine must be running first)
cd AssumptionChecker.Cli && dotnet run
# Custom engine URL:
dotnet run http://localhost:8080
```

## API Key Setup (Development)

The Engine reads the OpenAI API key from .NET User Secrets:

```bash
cd AssumptionChecker.Engine
dotnet user-secrets set "OpenAI:ApiKey" "sk-proj-..."
dotnet user-secrets set "OpenAI:Model" "gpt-4o-mini"   # optional override
```

User secrets ID: `06d15f90-2829-4db5-85a1-3030d1772639`

Production deployments use `WindowsSecureSettingsManager` (DPAPI) writing to `%APPDATA%\AssumptionChecker\settings.dat`.

## Architecture

```
VsExtension (net472) ──┐
WPFApp (net8.0-win)   ──┤── HTTP ──► Engine (net8.0, localhost:5046) ──► OpenAI API
Cli (net8.0)          ──┘
```

All clients use `AssumptionCheckerHttpService` from the **Core** library. Both **Core** and **Contracts** target `netstandard2.0 + net8.0` to support the net472 VS extension and net8.0 clients from the same source.

### Project Responsibilities

| Project | Target | Role |
|---|---|---|
| `Contracts` | netstandard2.0 + net8.0 | DTOs: `AnalyzeRequest`, `AnalyzeResponse`, `Assumption`, enums |
| `Core` | netstandard2.0 + net8.0 | `AssumptionCheckerHttpService`, `WindowsSecureSettingsManager`, DI helpers |
| `Engine` | net8.0 | ASP.NET Core Minimal API; `OpenAILlmClient` with retry/JSON parsing |
| `VsExtension` | net472 | VSIX package; auto-launches Engine; reads DTE for open file context |
| `WPFApp` | net8.0-windows | Standalone desktop app; auto-launches Engine; MVVM with chat UI |
| `Cli` | net8.0 | Interactive REPL with color-coded risk output |
| `Tests` | net8.0-windows | xUnit + Moq; covers HTTP service, ViewModel, settings, DTOs |

### Engine Endpoints

- `GET /health` → `{ "status": "healthy" }`
- `GET /templates` → list of analysis templates
- `POST /analyze` → accepts `AnalyzeRequest`, returns `AnalyzeResponse`

### Engine Launch Pattern

Both the VsExtension and WPFApp auto-launch the Engine on startup. They:
1. Look for `Engine/AssumptionChecker.Engine.exe` relative to their own executable
2. Start the process in the background
3. Poll `/health` (every 500ms, up to 5–10 seconds) before proceeding
4. Kill the Engine process on shutdown

### Key Implementation Details

- `OpenAILlmClient` forces JSON response format and retries up to 3 times if JSON parsing fails
- File context from IDE is truncated to 10,000 characters per file before sending to Engine
- `WindowsSecureSettingsManager` uses DPAPI (user-scoped); decryption failure returns null gracefully
- The VS Extension uses the DTE automation API to gather open file contents; this must run on the UI thread
- `ServiceCollectionExtensions.AddAssumptionChecker(baseUrl, timeoutSeconds)` is the DI entry point for clients

## Solution File

The solution uses the modern `.slnx` format (`AssumptionChecker.slnx`), not the traditional `.sln` format.

## Testing

Tests use **xUnit** and **Moq**. HTTP service tests use a custom fake `HttpMessageHandler`. ViewModel tests mock `IAssumptionCheckerService`. The test project targets `net8.0-windows` because it references the WPF `WPFApp` project.
