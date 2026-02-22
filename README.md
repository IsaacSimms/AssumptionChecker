# Assumption Checker for Visual Studio

> Analyzes prompts for hidden assumptions before they reach an AI system, then suggests improved alternatives — all inside Visual Studio.

---

## Table of Contents

- [How It Works](#how-it-works)
- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Setup: Configure Your OpenAI API Key](#setup-configure-your-openai-api-key)
- [Usage](#usage)
- [Advanced Configuration](#advanced-configuration)
- [Architecture Overview](#architecture-overview)
- [Troubleshooting](#troubleshooting)
- [License](#license)

---

## How It Works

When you type a prompt into the tool window, the extension:

1. Collects the content of all open documents in Visual Studio as additional context.
2. Sends the prompt and file context to a local **Engine** process (an ASP.NET Core API on http://localhost:5046).
3. The Engine forwards the request to the **OpenAI API** with a system prompt designed to surface critical assumptions.
4. Returns a list of identified assumptions (categorized and risk-rated) along with improved prompt suggestions.

The Engine process is **automatically launched** by the extension when Visual Studio starts. No manual server management is required under normal use.

---

## Prerequisites

| Requirement | Details |
|---|---|
| **Visual Studio** | 2022 (v17.0) or later, any edition (Community, Professional, Enterprise) |
| **.NET 8 SDK** | Required to run the Engine. Download at https://dotnet.microsoft.com/download/dotnet/8.0 |
| **OpenAI API Key** | A valid API key with access to chat completion models. Get one at https://platform.openai.com/api-keys |
| **Windows** | Required — the extension uses Windows DPAPI for secure API key storage |

---

## Installation

### Option A — Install the Pre-Built VSIX

1. Obtain the AssumptionChecker.VsExtension.vsix file (from a release or a teammate).
2. **Close all Visual Studio instances.**
3. Double-click the .vsix file and follow the VSIX Installer prompts.
4. Restart Visual Studio.

### Option B — Build from Source

1. Clone the repository:

        git clone https://github.com/IsaacSimms/AssumptionChecker.git
        cd AssumptionChecker

2. Build the Engine first (its binaries are bundled into the VSIX):

        dotnet build AssumptionChecker.Engine -c Release

3. Build the full solution:

        dotnet build -c Release

4. The VSIX file is produced at:

        AssumptionChecker.VsExtension\bin\Release\AssumptionChecker.VsExtension.vsix

5. Double-click the .vsix to install, or press **F5** with AssumptionChecker.VsExtension set as the startup project to launch the experimental instance for debugging.

---

## Setup: Configure Your OpenAI API Key

The Engine requires an OpenAI API key to function. You can provide it using either method below. **You only need to do one.**

### Method 1 — .NET User Secrets (Recommended for Development)

Best when you are building and running the Engine from source.

    cd AssumptionChecker.Engine

    # Initialize user secrets (already configured if you cloned the repo)
    dotnet user-secrets init

    # Set your OpenAI API key
    dotnet user-secrets set "OpenAI:ApiKey" "sk-proj-your-actual-api-key-here"

    # Verify it was saved
    dotnet user-secrets list

Expected output:

    OpenAI:ApiKey = sk-proj-...

User secrets are stored at %APPDATA%\Microsoft\UserSecrets\06d15f90-2829-4db5-85a1-3030d1772639\secrets.json and are **not** checked into source control.

### Method 2 — Encrypted Settings File (Recommended for Installed Extension)

Best when the extension is installed via VSIX and you don't have the source code open.

The Engine reads from an encrypted file at %APPDATA%\AssumptionChecker\settings.dat, protected with Windows DPAPI and scoped to your user account. To create this file, run the following one-time setup in a C# script, .NET Interactive notebook, or a small console app:

    using System.Security.Cryptography;
    using System.Text;

    var apiKey = "sk-proj-your-actual-api-key-here";
    var settingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AssumptionChecker");

    Directory.CreateDirectory(settingsDir);

    var plainBytes = Encoding.UTF8.GetBytes(apiKey);
    var encrypted = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
    File.WriteAllBytes(Path.Combine(settingsDir, "settings.dat"), encrypted);

    Console.WriteLine("API key saved successfully.");

The key is encrypted with your Windows user credentials. It cannot be decrypted by other users or on other machines.

---

## Usage

### Opening the Tool Window

1. In Visual Studio, go to the menu bar.
2. Click **Tools → Analyze Prompt Assumptions**.
3. The **Assumption Checker** tool window will appear. You can dock it like any other VS panel.

### Analyzing a Prompt

1. Type or paste a prompt into the text box at the top of the tool window.
   - Example: "Build a REST API for task management with authentication"
2. Click **Analyze Assumptions**.
3. Wait for the results — an "Analyzing..." indicator will appear while the request is in flight.

The extension automatically includes the content of all currently open documents as context (truncated to 10,000 characters per file), so the analysis is aware of the code you're working with.

### Understanding the Output

The results panel displays:

**Metadata** — Model used, latency in milliseconds, and number of assumptions found.

**Assumptions** — Each one includes:

| Field | Description |
|---|---|
| **Risk Level** | [High], [Medium], or [Low] — how much impact this assumption has if wrong |
| **Category** | UserContext, DomainContext, Constraints, OutputFormat, Ambiguity, or Other |
| **Rationale** | Why this assumption matters |
| **Ask** | A clarifying question to eliminate the assumption (shown only when applicable) |

**Suggested Improved Prompts** — 2–3 rewritten versions of your original prompt that are more specific and reduce ambiguity. These are complete, concrete, and copy-pastable (no placeholders).

### Using the CLI

A command-line interface is also available for quick testing without Visual Studio:

    # Make sure the Engine is running first
    cd AssumptionChecker.Cli
    dotnet run

Type prompts interactively at the > prompt. Type exit to quit.

---

## Advanced Configuration

### Custom Engine URL

By default the extension connects to http://localhost:5046. To override this, set an environment variable before launching Visual Studio:

    set ASSUMPTION_CHECKER_ENGINE_URL=http://localhost:8080

Or set it permanently via **Windows System Properties → Environment Variables**.

### Choosing a Different OpenAI Model

The default model is gpt-4o-mini. To use a different model:

    cd AssumptionChecker.Engine
    dotnet user-secrets set "OpenAI:Model" "gpt-4o"

| Model | Trade-off |
|---|---|
| gpt-4o-mini | **(Default)** Fast, cost-effective, good for most prompts |
| gpt-4o | Higher quality analysis, better at nuanced assumptions, higher cost |
| gpt-4-turbo | Previous generation, still capable |
| gpt-3.5-turbo | Fastest and cheapest, but may miss subtle assumptions |

### Running the Engine Manually

The extension auto-launches the Engine, but you can also start it yourself:

    cd AssumptionChecker.Engine
    dotnet run

You should see:

    info: Microsoft.Hosting.Lifetime[14]
          Now listening on: http://localhost:5046
    info: Microsoft.Hosting.Lifetime[0]
          Application started. Press Ctrl+C to shut down.

Verify it's running:

    curl http://localhost:5046/health

Expected response: {"status":"healthy"}

---

## Architecture Overview

    ┌─────────────────────────────────────────────────────────────┐
    │                      Visual Studio                          │
    │  ┌───────────────────────────────────────────────────────┐  │
    │  │          VsExtension (net472, VSIX)                   │  │
    │  │  ┌──────────────┐  ┌───────────────────────────────┐  │  │
    │  │  │ Tool Window   │→ │ AssumptionCheckerViewModel    │  │  │
    │  │  │ (WPF UI)      │  │ (gathers open files via DTE, │  │  │
    │  │  │               │  │  calls service, formats)      │  │  │
    │  │  └──────────────┘  └──────────────┬────────────────┘  │  │
    │  └────────────────────────────────────┼──────────────────┘  │
    │                                       │                     │
    │  ┌────────────────────────────────────▼──────────────────┐  │
    │  │          Core (netstandard2.0 + net8.0)               │  │
    │  │  IAssumptionCheckerService → HTTP POST /analyze       │  │
    │  │  WindowsSecureSettingsManager (DPAPI encryption)      │  │
    │  │  ResponseFormatter (Markdown output)                  │  │
    │  └────────────────────────────────────┬──────────────────┘  │
    └────────────────────────────────────────┼────────────────────┘
                                            │ HTTP
                                            ▼
    ┌─────────────────────────────────────────────────────────────┐
    │          Engine (net8.0, ASP.NET Core Minimal API)          │
    │  localhost:5046                                             │
    │  ┌───────────────────────────────────────────────────────┐  │
    │  │ OpenAILlmClient                                       │  │
    │  │  • Builds system prompt for assumption analysis       │  │
    │  │  • Sends to OpenAI Chat Completions API               │  │
    │  │  • Parses JSON response (retries up to 3x on failure) │  │
    │  └───────────────────────────┬───────────────────────────┘  │
    └──────────────────────────────┼──────────────────────────────┘
                                   │ HTTPS
                                   ▼
                          ┌──────────────────┐
                          │   OpenAI API     │
                          │  (gpt-4o-mini)   │
                          └──────────────────┘

    Shared: Contracts (netstandard2.0 + net8.0)
      AnalyzeRequest, AnalyzeResponse, Assumption, ResponseMetadata, Enums

| Project | Target | Role |
|---|---|---|
| AssumptionChecker.Contracts | netstandard2.0 + net8.0 | Shared DTOs and enums |
| AssumptionChecker.Core | netstandard2.0 + net8.0 | HTTP client, DI wiring, secure storage, response formatting |
| AssumptionChecker.Engine | net8.0 | ASP.NET Core API that calls OpenAI and returns structured assumptions |
| AssumptionChecker.VsExtension | net472 | VSIX extension — WPF tool window, auto-launches Engine, gathers file context |
| AssumptionChecker.Cli | net8.0 | Interactive command-line client for testing |

---

## Troubleshooting

### "ERROR: ... Make sure the Engine is running"

The Engine process failed to start or isn't reachable.

1. Check if the Engine is running: curl http://localhost:5046/health
2. Start it manually: cd AssumptionChecker.Engine && dotnet run
3. Check the Visual Studio **Output** window (**View → Output**) for [AssumptionChecker] log messages.

### "OpenAI:ApiKey is not configured"

The Engine cannot find your API key.

1. Verify user secrets: cd AssumptionChecker.Engine && dotnet user-secrets list — you should see OpenAI:ApiKey = sk-proj-...
2. Or verify the encrypted settings file exists at %APPDATA%\AssumptionChecker\settings.dat
3. If neither exists, follow the Setup steps above.

### "LLM failed to return valid JSON (includes reattempts)"

The OpenAI model returned malformed output after 3 retry attempts. This is rare but can happen with less capable models. Switch to a stronger model:

    cd AssumptionChecker.Engine
    dotnet user-secrets set "OpenAI:Model" "gpt-4o"

### The menu item "Analyze Prompt Assumptions" doesn't appear

1. Go to **Extensions → Manage Extensions** and confirm **Assumption Checker for Copilot** is listed and enabled.
2. If you just installed the VSIX, make sure you **restarted Visual Studio**.
3. If running from source, ensure the startup project is AssumptionChecker.VsExtension and launch with **F5** (uses the /rootsuffix Exp experimental instance).

### Port 5046 is already in use

Another process is using the default port. Either stop the conflicting process, or set a custom URL:

    set ASSUMPTION_CHECKER_ENGINE_URL=http://localhost:9090
    set ASPNETCORE_URLS=http://localhost:9090
    cd AssumptionChecker.Engine
    dotnet run

### Analysis is slow

- The first request may be slower due to Engine cold-start and OpenAI API latency.
- Subsequent requests are faster. Typical latency is shown in the results metadata.
- gpt-4o-mini (the default) is the fastest option.
- Open file context is truncated to 10,000 characters per file. Closing unnecessary files reduces payload size.

---

## License

See [LICENSE](LICENSE) for details.
