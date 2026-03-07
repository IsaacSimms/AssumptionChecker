# Assumption Checker for Visual Studio

> Analyzes Copilot prompts for hidden assumptions and suggests improved alternatives — directly inside Visual Studio 2022.

---

## Table of Contents

- [How It Works](#how-it-works)
- [Prerequisites](#prerequisites)
- [Quick Install](#quick-install)
- [Usage](#usage)
- [Advanced Configuration](#advanced-configuration)
- [Architecture Overview](#architecture-overview)
- [Troubleshooting](#troubleshooting)

---

## How It Works

```
VS Extension  ──►  Engine (localhost:5046)  ──►  OpenAI API
   (net472)          (ASP.NET Core net8)
```

1. You type a prompt into the tool window.
2. The extension attaches all open documents as file context (truncated to 10k chars/file).
3. It sends both to a local ASP.NET Core engine, which calls the OpenAI API.
4. Results come back as a structured list of assumptions (risk-rated and categorized) plus suggested improved prompts.

The engine auto-starts when VS loads — no manual server management required under normal use.

---

## Prerequisites

| Requirement | Details |
|---|---|
| **Visual Studio 2022** | v17.x — Community, Pro, or Enterprise |
| **.NET 8 SDK** | [dotnet.microsoft.com/download/dotnet/8.0](https://dotnet.microsoft.com/download/dotnet/8.0) |
| **OpenAI API Key** | [platform.openai.com/api-keys](https://platform.openai.com/api-keys) |
| **Windows** | Required — API key is stored with Windows DPAPI |

---

## Quick Install

### 1. Clone the repo

```bash
git clone https://github.com/IsaacSimms/AssumptionChecker.git
cd AssumptionChecker
```

### 2. Save your OpenAI API key

>  dotnet user-secrets (source builds only)**
> ```bash
> cd AssumptionChecker.Engine
> dotnet user-secrets set "OpenAI:ApiKey" "sk-proj-YOUR-KEY-HERE"
> ```

 **Alternative — Run the WPF companion app, go to the **Settings** tab, paste your key, and click **Save**. This encrypts the key with Windows DPAPI at `%AppData%\AssumptionChecker\settings.dat`, which the engine reads on every startup.

```bash
cd AssumptionChecker.WPFApp
dotnet run
```


### 3. Build the engine

```bash
cd AssumptionChecker.Engine
dotnet build -c Release
```

> For a permanent VSIX install, the engine `.exe` must be built before packaging so the extension can auto-launch it. The extension looks for it at `Engine\AssumptionChecker.Engine.exe` next to the extension DLL.

### 4. Install the extension

**Option A — F5 experimental instance (quickest for development)**

1. Open `AssumptionChecker.sln` in Visual Studio 2022.
2. Right-click `AssumptionChecker.VsExtension` → **Set as Startup Project**.
3. Press **F5** — a second VS instance opens with the extension already loaded.

**Option B — build and install the VSIX (permanent)**

```bash
dotnet build AssumptionChecker.sln -c Release
```

Double-click the generated file:

```
AssumptionChecker.VsExtension\bin\Release\AssumptionChecker.VsExtension.vsix
```

Follow the installer prompts, then restart Visual Studio.

### 5. Open the tool window

**View → Other Windows → Assumption Checker**

Dock the panel wherever you like. The engine starts automatically in the background.

---

## Usage

| Action | How |
|---|---|
| Submit prompt | Type and press **Enter** |
| Insert a newline | **Shift + Enter** |
| Submit with mouse | Click **Analyze Assumptions** |

Results include each assumption's **risk level** (`[High]` / `[Medium]` / `[Low]`), **category**, **rationale**, a **clarifying question**, and 2–3 complete **suggested improved prompts** ready to copy-paste.

### CLI (optional — for testing without VS)

Make sure the engine is running first, then:

```bash
cd AssumptionChecker.Cli
dotnet run
```

Type prompts at the `>` prompt. Type `exit` to quit.

---

## Advanced Configuration

### Custom engine URL

Set this environment variable before launching Visual Studio to point the extension at a different host or port:

```powershell
$env:ASSUMPTION_CHECKER_ENGINE_URL = "http://localhost:9090"
```

The engine must also be told to listen on that port:

```bash
cd AssumptionChecker.Engine
$env:ASPNETCORE_URLS = "http://localhost:9090"
dotnet run
```

### Changing the OpenAI model

The default is `gpt-4o-mini`. Override with user secrets:

```bash
cd AssumptionChecker.Engine
dotnet user-secrets set "OpenAI:Model" "gpt-4o"
```

| Model | Trade-off |
|---|---|
| `gpt-4o-mini` | **(Default)** Fast, low cost, solid for most prompts |
| `gpt-4o` | Higher quality, better nuance, higher cost |

---

## Architecture Overview

```
┌──────────────────────────────────────────────────────────────┐
│                       Visual Studio                          │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  VsExtension (net472, VSIX)                            │  │
│  │  Tool Window (WPF) → ViewModel                         │  │
│  │    • gathers open files via DTE                        │  │
│  │    • calls IAssumptionCheckerService                   │  │
│  └──────────────────────────┬─────────────────────────────┘  │
└─────────────────────────────┼────────────────────────────────┘
                              │ HTTP POST /analyze
┌─────────────────────────────▼────────────────────────────────┐
│  Engine (net8.0, ASP.NET Core)  localhost:5046               │
│  OpenAILlmClient                                             │
│    • builds system prompt                                    │
│    • calls OpenAI Chat Completions API                       │
│    • parses JSON (retries up to 3x on malformed response)    │
└──────────────────────────────────────────────────────────────┘
```

| Project | Target | Role |
|---|---|---|
| `AssumptionChecker.Contracts` | netstandard2.0 / net8.0 | Shared DTOs and enums |
| `AssumptionChecker.Core` | netstandard2.0 / net8.0 | HTTP client, DI wiring, DPAPI key storage |
| `AssumptionChecker.Engine` | net8.0 | ASP.NET Core API — calls OpenAI, returns structured JSON |
| `AssumptionChecker.VsExtension` | net472 | VSIX — WPF tool window, auto-launches engine, gathers file context |
| `AssumptionChecker.WPFApp` | net8.0 | Standalone WPF chat UI; also used to save the API key |
| `AssumptionChecker.Cli` | net8.0 | Interactive CLI for testing the engine |
| `AssumptionChecker.Tests` | net8.0 | Unit tests |

---

## Troubleshooting

**"ERROR: Make sure the Engine is running"**
The extension couldn't reach `http://localhost:5046`. Start it manually:
```bash
cd AssumptionChecker.Engine && dotnet run
```
Then check the VS **Output** window for `[AssumptionChecker]` log lines.

**Engine starts but returns 500 / "OpenAI:ApiKey is not configured"**
The API key is missing. Re-run the WPF app, save your key in Settings, then restart the engine. Or verify user secrets:
```bash
cd AssumptionChecker.Engine && dotnet user-secrets list
```

**VSIX won't install**
Confirm you are on Visual Studio 2022 (v17.x). The manifest targets `[17.0, 19.0)`.

**"Assumption Checker" doesn't appear in View → Other Windows**
Go to **Extensions → Manage Extensions**, confirm the extension is enabled, and restart VS. If running from source, press **F5** from the `AssumptionChecker.VsExtension` startup project.

**Port 5046 is already in use**
Set a custom URL (see [Advanced Configuration](#advanced-configuration)) or stop the conflicting process.

**LLM returns malformed JSON repeatedly**
Switch to a stronger model:
```bash
cd AssumptionChecker.Engine
dotnet user-secrets set "OpenAI:Model" "gpt-4o"
```
---

## License

See [LICENSE](LICENSE) for details.
