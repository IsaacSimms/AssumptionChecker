# Assumption Checker

> Analyzes Copilot prompts for hidden assumptions and suggests improved alternatives. Available as a **Visual Studio 2022 extension** and a **standalone WPF desktop app**. Supports both **OpenAI** and **Anthropic** models.

---

## Table of Contents

- [How It Works](#how-it-works)
- [Prerequisites](#prerequisites)
- [Quick Install (VS Extension)](#quick-install-vs-extension)
- [WPF Standalone App](#wpf-standalone-app)
- [Usage](#usage)
- [Advanced Configuration](#advanced-configuration)
- [Architecture Overview](#architecture-overview)
- [Troubleshooting](#troubleshooting)

---

## How It Works

```
                         ┌──► OpenAI API   (gpt-4o-mini, gpt-4o, …)
VS Extension  ──►  Engine│
   (net472)    (localhost:5046)
WPF App       ──►        └──► Anthropic API (claude-sonnet-4, claude-haiku-4, …)
   (net8.0)
```

1. You type a prompt into the tool window and choose a model.
2. The extension attaches all open documents as file context (truncated to 10k chars/file).
3. It sends both to a local ASP.NET Core engine, which routes to the correct LLM provider.
4. Results come back as a structured list of assumptions (risk-rated and categorized) plus suggested improved prompts.

The engine auto-starts when VS loads. A **`LlmClientRouter`** inspects the model name — anything starting with `claude` goes to `AnthropicLlmClient`, everything else goes to `OpenAILlmClient`.

---

## Prerequisites

| Requirement | Details |
|---|---|
| **Visual Studio 2022 or 2026** | v17.x — Community, Pro, or Enterprise (VS Extension only) |
| **.NET 8 SDK** | [dotnet.microsoft.com/download/dotnet/8.0](https://dotnet.microsoft.com/download/dotnet/8.0) (source builds only — the MSI bundles the runtime) |
| **API Key** | At least one: [OpenAI](https://platform.openai.com/api-keys) and/or [Anthropic](https://console.anthropic.com/settings/keys) |
| **Windows** | Required — API keys are stored with Windows DPAPI |

---

## Quick Install (VS Extension)

### 1. Clone the repo

```bash
git clone https://github.com/IsaacSimms/AssumptionChecker.git
cd AssumptionChecker
```

### 2. Build the engine

```bash
cd AssumptionChecker.Engine
dotnet build -c Release
```

> For a permanent VSIX install the engine `.exe` must be built first so the extension can auto-launch it.

### 3. Install the extension

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

### 4. Open the tool window and add your API key(s)

**View → Other Windows → Assumption Checker**

Expand the **API Key Settings** panel at the top of the tool window. Paste your key and click **Save** for each provider you want to use:

| Provider | Key prefix | Where to get one |
|---|---|---|
| OpenAI | `sk-proj-…` | [platform.openai.com/api-keys](https://platform.openai.com/api-keys) |
| Anthropic | `sk-ant-…` | [console.anthropic.com/settings/keys](https://console.anthropic.com/settings/keys) |

Keys are encrypted with Windows DPAPI (per-user) and saved to `%AppData%\AssumptionChecker\`. A green **"Configured"** badge appears once a key is saved. The engine picks up the new key immediately — no restart required.

> **Alternative — dotnet user-secrets (source builds only)**
> ```bash
> cd AssumptionChecker.Engine
> dotnet user-secrets set "OpenAI:ApiKey" "sk-proj-YOUR-KEY-HERE"
> dotnet user-secrets set "Anthropic:ApiKey" "sk-ant-YOUR-KEY-HERE"
> ```

> **Alternative — WPF companion app**
> The standalone WPF app also has a Settings tab that writes to the same DPAPI-encrypted files:
> ```bash
> cd AssumptionChecker.WPFApp
> dotnet run
> ```

---

## WPF Standalone App

The standalone WPF app provides the same assumption-checking experience outside of Visual Studio. It bundles its own Engine process and launches it automatically on startup.

### Install from MSI (recommended)

1. Download **`AssumptionChecker.Installer.msi`** from the [Releases](https://github.com/IsaacSimms/AssumptionChecker/releases) page.
2. Double-click the `.msi` — no admin rights required.
3. The app installs to `%LocalAppData%\AssumptionChecker` with Start Menu and Desktop shortcuts.
4. Launch **Assumption Checker** from the Start Menu.

> **Silent / unattended install**
> The MSI supports a fully silent install with no UI prompts — useful for scripted deployments or IT provisioning:
> ```powershell
> msiexec /i AssumptionChecker.Installer.msi /quiet /norestart
> ```
> Use `/passive` instead of `/quiet` to show a minimal progress bar with no interaction required.

> The MSI is a self-contained package — it includes the .NET 8 runtime, all libraries, and the Engine executable. No separate SDK or runtime install is needed.

### Install from source

```bash
git clone https://github.com/IsaacSimms/AssumptionChecker.git
cd AssumptionChecker\AssumptionChecker.WPFApp
dotnet run
```

This requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0). The Engine is built and copied automatically on first run.

### Configure API keys

1. Open the app and expand the **Settings** panel.
2. Paste your API key for each provider you want to use and click **Save**.

| Provider | Key prefix | Where to get one |
|---|---|---|
| OpenAI | `sk-proj-…` | [platform.openai.com/api-keys](https://platform.openai.com/api-keys) |
| Anthropic | `sk-ant-…` | [console.anthropic.com/settings/keys](https://console.anthropic.com/settings/keys) |

Keys are encrypted with **Windows DPAPI** (per-user) and stored in `%AppData%\AssumptionChecker\`. They are shared with the VS Extension — configuring a key in either place makes it available to both.

### Build the MSI from source

Prerequisites (one-time):

```powershell
dotnet tool install --global wix
wix extension add WixToolset.Heat/4.0.5
```

Build:

```powershell
cd AssumptionChecker\AssumptionChecker.Installer
.\build-installer.ps1
```

This publishes the WPF app and Engine as self-contained win-x64, then produces the MSI at:

```
AssumptionChecker.Installer\bin\Release\AssumptionChecker.Installer.msi
```

### Uninstall

Uninstall via **Settings → Apps → Assumption Checker** (or Programs and Features). The installer removes all application files and cached preferences (`wpf-settings.json`) but **preserves your DPAPI-encrypted API keys** in `%AppData%\AssumptionChecker\` so they remain available if you reinstall or continue using the VS Extension.

---

## Usage

### Analyzing a prompt

| Action | How |
|---|---|
| Submit prompt | Type and press **Enter** |
| Insert a newline | **Shift + Enter** |
| Submit with mouse | Click **Analyze Assumptions** |

### Choosing a model

Use the **Model** dropdown above the Analyze button. The extension ships with models from both providers:

| Provider | Models |
|---|---|
| **OpenAI** | `gpt-4o-mini` **(default)**, `gpt-4o`, `o1-mini`, `o1`, `gpt-5.2`, `gpt-5-mini` |
| **Anthropic** | `claude-sonnet-4-6`, `claude-haiku-4-5`, `claude-opus-4-6` |

Selecting a `claude-*` model routes to Anthropic; everything else routes to OpenAI. The engine verifies the corresponding API key is configured before sending the request.

### Understanding the output

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

Set this environment variable before launching Visual Studio:

```powershell
$env:ASSUMPTION_CHECKER_ENGINE_URL = "http://localhost:9090"
```

The engine must also be told to listen on that port:

```bash
cd AssumptionChecker.Engine
$env:ASPNETCORE_URLS = "http://localhost:9090"
dotnet run
```

### API key storage details

| Provider | File | Format |
|---|---|---|
| OpenAI | `%AppData%\AssumptionChecker\settings.dat` | DPAPI-encrypted (legacy path, kept for backward compat) |
| Anthropic | `%AppData%\AssumptionChecker\settings-anthropic.dat` | DPAPI-encrypted |

Keys can be saved three ways (all write to the same files):
1. **VS Extension** — API Key Settings panel (calls `POST /settings/apikey` on the engine)
2. **WPF app** — Settings tab
3. **dotnet user-secrets** — development override (read by the engine's ASP.NET Core config chain)

The engine **hot-reloads** keys saved via the `/settings/apikey` endpoint — no restart needed.

---

## Architecture Overview

```
┌──────────────────────────────────────────────────────────────┐
│                       Visual Studio                          │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  VsExtension (net472, VSIX)                            │  │
│  │  Tool Window (WPF) → ViewModel                         │  │
│  │    • model selector + API key settings panel           │  │
│  │    • gathers open files via DTE                        │  │
│  │    • calls IAssumptionCheckerService                   │  │
│  │    • saves keys via POST /settings/apikey              │  │
│  └──────────────────────────┬─────────────────────────────┘  │
└─────────────────────────────┼────────────────────────────────┘
                              │ HTTP
┌─────────────────────────────▼────────────────────────────────┐
│  Engine (net8.0, ASP.NET Core)  localhost:5046               │
│                                                              │
│  POST /analyze ──► LlmClientRouter                           │
│                       ├── claude-* ──► AnthropicLlmClient    │
│                       └── *        ──► OpenAILlmClient       │
│                                                              │
│  POST /settings/apikey   (save key, hot-reload into config)  │
│  GET  /settings/providers (check which keys are configured)  │
│  GET  /health                                                │
└──────────────────────────────────────────────────────────────┘
```

| Project | Target | Role |
|---|---|---|
| `AssumptionChecker.Contracts` | netstandard2.0 / net8.0 | Shared DTOs and enums |
| `AssumptionChecker.Core` | netstandard2.0 / net8.0 | HTTP client, DI wiring, multi-provider DPAPI key storage |
| `AssumptionChecker.Engine` | net8.0 | ASP.NET Core API — LLM router, OpenAI + Anthropic clients, settings endpoints |
| `AssumptionChecker.VsExtension` | net472 | VSIX — WPF tool window, model picker, key management, auto-launches engine |
| `AssumptionChecker.WPFApp` | net8.0 | Standalone WPF chat UI with settings |
| `AssumptionChecker.Installer` | WiX v4 | MSI installer — bundles WPFApp + Engine as self-contained win-x64 |
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

**"OpenAI API key is not configured" / "Anthropic API key is not configured"**
Open the tool window → expand **API Key Settings** → paste and save the key for the provider you selected. The green "Configured" badge should appear and the engine hot-reloads the key immediately.

**VSIX won't install**
Confirm you are on Visual Studio 2022 (v17.x). The manifest targets `[17.0, 19.0)`.

**"Assumption Checker" doesn't appear in View → Other Windows**
Go to **Extensions → Manage Extensions**, confirm the extension is enabled, and restart VS. If running from source, press **F5** from the `AssumptionChecker.VsExtension` startup project.

**Port 5046 is already in use**
Set a custom URL (see [Advanced Configuration](#advanced-configuration)) or stop the conflicting process.

**LLM returns malformed JSON repeatedly**
The model returned invalid output after 3 retry attempts. Try a stronger model from the dropdown (e.g. `gpt-4o` or `claude-sonnet-4-6`).

---

## License

See [LICENSE](LICENSE) for details.
