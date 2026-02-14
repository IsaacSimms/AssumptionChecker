## Setup Instructions

### Prerequisites
- .NET 8 SDK
- OpenAI API account with API key
- Visual Studio 2026 or later

### Step 1: Configure the Engine

1. **Clone or download** the AssumptionChecker repository
2. **Navigate to the Engine project:**
   ```bash
   cd C:\AssumptionChecker\AssumptionChecker\AssumptionChecker.Engine
   ```

3. **Set your OpenAI API key** using .NET User Secrets:

# Initialize user secrets (if not already done)
   ```bash
dotnet user-secrets init
   ```
# Set your OpenAI API key
   ```bash
dotnet user-secrets set "OpenAI:ApiKey" "sk-proj-your-actual-api-key-here"
   ```

# Verify that it worked by running:
   ```bash
dotnet user-secrets list
   ```
  You should see:
     ```bash
OpenAI:ApiKey = sk-proj-...
      ```
4. **(Optional) Choose your OpenAI model:**
   ```bash
   dotnet user-secrets set "OpenAI:Model" "gpt-4o"
   ```
   Default: `gpt-4o-mini` (cheaper, faster)

5. **Start the Engine:**
 # From the engine directory
   ```bash
   dotnet run
   ```
   
   You should see:
   ```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5046
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
   ```

### Step 2: Install the VS Extension

1. Build the extension project in Release mode
2. Locate `AssumptionChecker.VsExtension.vsix` in `bin/Release/net8.0-windows8.0/`
3. Double-click to install
4. Restart Visual Studio

### Step 3: Configure the Extension

1. In Visual Studio, go to __Tools > Options__
2. Navigate to __Assumption Checker__
3. Set __Engine URL__ to `http://localhost:5046` (or your custom endpoint)
4. Click __OK__

### Step 4: Use in Copilot Chat

1. Open Copilot Chat
2. Type: `@assumptioncheck Build a REST API for task management`
3. Review assumptions and select an improved prompt option [0-3]

---

## Advanced Configuration

### Running the Engine on a Different Port

```bash
# Set custom port via environment variable
set ASPNETCORE_URLS=http://localhost:8080
dotnet run

# Then update VS Extension settings to point to http://localhost:8080
```

### Using a Different OpenAI Model

Supported models:
- `gpt-4o-mini` (default) - Fast, cost-effective
- `gpt-4o` - More capable, higher quality
- `gpt-4-turbo` - Previous generation
- `gpt-3.5-turbo` - Fastest, cheapest (may miss nuanced assumptions)

```bash
dotnet user-secrets set "OpenAI:Model" "gpt-4o"
```

### Cloud Deployment (Optional)

To deploy the Engine to Azure and share across multiple machines:

1. Deploy `AssumptionChecker.Engine` to Azure App Service
2. Configure your OpenAI API key in Azure __Configuration > Application settings__
3. Update VS Extension settings to point to your Azure URL: `https://your-app.azurewebsites.net`

---

## Troubleshooting

### "Could not reach the AssumptionChecker Engine"

**Cause:** The Engine is not running.

**Solution:**
1. Open a terminal in `AssumptionChecker.Engine`
2. Run `dotnet run`
3. Verify you see "Now listening on: http://localhost:5046"

### "OpenAI:ApiKey is not configured"

**Cause:** No API key set in user secrets.

**Solution:**
```bash
cd AssumptionChecker.Engine
dotnet user-secrets set "OpenAI:ApiKey" "sk-your-key"
```

### Extension not showing in Copilot Chat

**Cause:** Extension not loaded or VS needs restart.

**Solution:**
1. __Extensions > Manage Extensions__
2. Verify "Assumption Checker for Copilot" is installed and enabled
3. Restart Visual Studio
