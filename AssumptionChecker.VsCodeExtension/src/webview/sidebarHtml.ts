// <summary>
// Generates the complete HTML/CSS/JS for the sidebar webview panel.
// Uses VS Code CSS variables for theme integration.
// Sections: model selector, API key settings, prompt input, analyze button, results.
// CSP uses nonce-based script tag for security.
// </summary>

// == getSidebarHtml == //
export function getSidebarHtml(nonce: string, models: string[], defaultModel: string): string {
    const modelOptions = models
        .map(m => `<option value="${m}" ${m === defaultModel ? "selected" : ""}>${m}</option>`)
        .join("\n");

    return /*html*/ `<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8" />
    <meta http-equiv="Content-Security-Policy"
          content="default-src 'none'; style-src 'unsafe-inline'; script-src 'nonce-${nonce}';" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>AssumptionChecker</title>
    <style>
        /* == base reset == */
        * { box-sizing: border-box; margin: 0; padding: 0; }
        body {
            font-family: var(--vscode-font-family);
            font-size: var(--vscode-font-size);
            color: var(--vscode-foreground);
            background: var(--vscode-sideBar-background);
            padding: 12px;
        }

        /* == section spacing == */
        .section { margin-bottom: 14px; }
        label {
            display: block;
            font-weight: 600;
            margin-bottom: 4px;
            font-size: 11px;
            text-transform: uppercase;
            color: var(--vscode-descriptionForeground);
        }

        /* == inputs & selects == */
        select, textarea, input[type="password"] {
            width: 100%;
            padding: 6px 8px;
            border: 1px solid var(--vscode-input-border);
            background: var(--vscode-input-background);
            color: var(--vscode-input-foreground);
            border-radius: 2px;
            font-family: inherit;
            font-size: inherit;
        }
        textarea { resize: vertical; min-height: 80px; }
        select { cursor: pointer; }

        /* == buttons == */
        button {
            padding: 6px 14px;
            border: none;
            border-radius: 2px;
            cursor: pointer;
            font-size: inherit;
            font-family: inherit;
        }
        .btn-primary {
            width: 100%;
            background: var(--vscode-button-background);
            color: var(--vscode-button-foreground);
            font-weight: 600;
        }
        .btn-primary:hover { background: var(--vscode-button-hoverBackground); }
        .btn-primary:disabled { opacity: 0.5; cursor: not-allowed; }
        .btn-small {
            padding: 3px 10px;
            background: var(--vscode-button-secondaryBackground);
            color: var(--vscode-button-secondaryForeground);
            font-size: 12px;
        }
        .btn-small:hover { background: var(--vscode-button-secondaryHoverBackground); }

        /* == settings panel == */
        details {
            margin-bottom: 14px;
            border: 1px solid var(--vscode-panel-border);
            border-radius: 2px;
        }
        summary {
            padding: 8px 10px;
            cursor: pointer;
            font-weight: 600;
            font-size: 12px;
            user-select: none;
            background: var(--vscode-sideBarSectionHeader-background);
        }
        .settings-body { padding: 10px; }
        .key-row {
            display: flex;
            align-items: center;
            gap: 6px;
            margin-bottom: 8px;
        }
        .key-row label { flex: 0 0 70px; margin-bottom: 0; text-transform: none; font-size: inherit; }
        .key-row input { flex: 1; }
        .status-dot {
            width: 10px; height: 10px;
            border-radius: 50%;
            flex-shrink: 0;
        }
        .status-dot.configured { background: var(--vscode-testing-iconPassed); }
        .status-dot.missing    { background: var(--vscode-testing-iconFailed); }
        .settings-msg {
            font-size: 12px;
            margin-top: 4px;
            color: var(--vscode-descriptionForeground);
        }

        /* == spinner == */
        .spinner {
            display: none;
            align-items: center;
            gap: 8px;
            margin-top: 8px;
            color: var(--vscode-descriptionForeground);
        }
        .spinner.visible { display: flex; }

        /* == results == */
        #results { display: none; margin-top: 14px; }
        #results.visible { display: block; }

        .results-meta {
            font-size: 11px;
            color: var(--vscode-descriptionForeground);
            margin-bottom: 10px;
            padding: 6px 8px;
            background: var(--vscode-textBlockQuote-background);
            border-radius: 2px;
        }

        /* == assumption cards == */
        .assumption-card {
            border-left: 3px solid;
            padding: 8px 10px;
            margin-bottom: 8px;
            background: var(--vscode-editor-background);
            border-radius: 0 2px 2px 0;
        }
        .assumption-card.high   { border-left-color: var(--vscode-errorForeground); }
        .assumption-card.medium { border-left-color: var(--vscode-editorWarning-foreground); }
        .assumption-card.low    { border-left-color: var(--vscode-editorInfo-foreground); }

        .card-header {
            display: flex;
            align-items: center;
            gap: 6px;
            margin-bottom: 4px;
        }
        .risk-badge {
            font-size: 10px;
            font-weight: 700;
            text-transform: uppercase;
            padding: 1px 6px;
            border-radius: 2px;
        }
        .risk-badge.high   { background: var(--vscode-errorForeground); color: #fff; }
        .risk-badge.medium { background: var(--vscode-editorWarning-foreground); color: #000; }
        .risk-badge.low    { background: var(--vscode-editorInfo-foreground); color: #fff; }

        .category-badge {
            font-size: 10px;
            padding: 1px 6px;
            border-radius: 2px;
            background: var(--vscode-badge-background);
            color: var(--vscode-badge-foreground);
        }
        .confidence {
            font-size: 10px;
            margin-left: auto;
            color: var(--vscode-descriptionForeground);
        }
        .assumption-text { font-weight: 600; margin-bottom: 4px; }
        .rationale { font-size: 12px; color: var(--vscode-descriptionForeground); margin-bottom: 4px; }
        .clarifying {
            font-size: 12px;
            font-style: italic;
            color: var(--vscode-textLink-foreground);
        }

        /* == suggested prompts == */
        .suggested-section { margin-top: 14px; }
        .suggested-section h4 {
            font-size: 12px;
            margin-bottom: 6px;
            color: var(--vscode-descriptionForeground);
        }
        .suggested-prompt {
            padding: 6px 8px;
            margin-bottom: 4px;
            background: var(--vscode-textBlockQuote-background);
            border-radius: 2px;
            font-size: 12px;
            cursor: pointer;
        }
        .suggested-prompt:hover {
            background: var(--vscode-list-hoverBackground);
        }
    </style>
</head>
<body>
    <!-- == Model Selector == -->
    <div class="section">
        <label>Model</label>
        <select id="modelSelect">${modelOptions}</select>
    </div>

    <!-- == API Key Settings == -->
    <details id="settingsPanel">
        <summary>API Key Settings</summary>
        <div class="settings-body">
            <div class="key-row">
                <label>OpenAI</label>
                <input type="password" id="openaiKey" placeholder="sk-..." />
                <button class="btn-small" id="saveOpenai">Save</button>
                <span id="openaiDot" class="status-dot missing"></span>
            </div>
            <div class="key-row">
                <label>Anthropic</label>
                <input type="password" id="anthropicKey" placeholder="sk-ant-..." />
                <button class="btn-small" id="saveAnthropic">Save</button>
                <span id="anthropicDot" class="status-dot missing"></span>
            </div>
            <div id="settingsMsg" class="settings-msg"></div>
        </div>
    </details>

    <!-- == Prompt Input == -->
    <div class="section">
        <label>Prompt</label>
        <textarea id="promptInput" rows="5"
                  placeholder="Enter a prompt to analyze for hidden assumptions..."></textarea>
    </div>

    <!-- == Analyze Button == -->
    <button class="btn-primary" id="analyzeBtn">Analyze Assumptions</button>
    <div class="spinner" id="spinner">
        <span>Analyzing...</span>
    </div>

    <!-- == Results == -->
    <div id="results">
        <div id="resultsMeta" class="results-meta"></div>
        <div id="assumptionsList"></div>
        <div id="suggestedSection" class="suggested-section" style="display:none;">
            <h4>Suggested Improved Prompts</h4>
            <div id="suggestedList"></div>
        </div>
    </div>

    <script nonce="${nonce}">
        // == acquire VS Code API == //
        const vscode = acquireVsCodeApi();

        // == DOM refs == //
        const modelSelect     = document.getElementById("modelSelect");
        const openaiKeyInput  = document.getElementById("openaiKey");
        const anthropicKeyInput = document.getElementById("anthropicKey");
        const saveOpenaiBtn   = document.getElementById("saveOpenai");
        const saveAnthropicBtn = document.getElementById("saveAnthropic");
        const openaiDot       = document.getElementById("openaiDot");
        const anthropicDot    = document.getElementById("anthropicDot");
        const settingsMsg     = document.getElementById("settingsMsg");
        const promptInput     = document.getElementById("promptInput");
        const analyzeBtn      = document.getElementById("analyzeBtn");
        const spinner         = document.getElementById("spinner");
        const resultsDiv      = document.getElementById("results");
        const resultsMeta     = document.getElementById("resultsMeta");
        const assumptionsList = document.getElementById("assumptionsList");
        const suggestedSection = document.getElementById("suggestedSection");
        const suggestedList   = document.getElementById("suggestedList");

        // == restore persisted state == //
        const previousState = vscode.getState();
        if (previousState) {
            if (previousState.prompt) { promptInput.value = previousState.prompt; }
            if (previousState.model)  { modelSelect.value = previousState.model; }
        }

        // == persist state on changes == //
        promptInput.addEventListener("input", () => {
            vscode.setState({ ...vscode.getState(), prompt: promptInput.value });
        });
        modelSelect.addEventListener("change", () => {
            vscode.setState({ ...vscode.getState(), model: modelSelect.value });
        });

        // == request initial state from extension == //
        vscode.postMessage({ command: "getInitialState" });

        // == analyze button == //
        analyzeBtn.addEventListener("click", () => sendAnalyze());

        // == Enter to submit, Shift+Enter for newline == //
        promptInput.addEventListener("keydown", (e) => {
            if (e.key === "Enter" && !e.shiftKey) {
                e.preventDefault();
                sendAnalyze();
            }
        });

        function sendAnalyze() {
            const prompt = promptInput.value.trim();
            if (!prompt) { return; }

            analyzeBtn.disabled = true;
            spinner.classList.add("visible");
            resultsDiv.classList.remove("visible");

            vscode.postMessage({
                command: "analyze",
                payload: {
                    prompt: prompt,
                    model: modelSelect.value,
                    maxAssumptions: 10,
                },
            });
        }

        // == save API key buttons == //
        saveOpenaiBtn.addEventListener("click", () => {
            const key = openaiKeyInput.value.trim();
            if (!key) { return; }
            vscode.postMessage({ command: "saveApiKey", payload: { provider: "openai", apiKey: key } });
        });
        saveAnthropicBtn.addEventListener("click", () => {
            const key = anthropicKeyInput.value.trim();
            if (!key) { return; }
            vscode.postMessage({ command: "saveApiKey", payload: { provider: "anthropic", apiKey: key } });
        });

        // == handle messages from extension == //
        window.addEventListener("message", (event) => {
            const msg = event.data;
            switch (msg.command) {
                case "analyzeResult":
                    renderResults(msg.payload);
                    break;
                case "analyzeError":
                    renderError(msg.payload);
                    break;
                case "providerStatus":
                    updateProviderDots(msg.payload);
                    break;
                case "apiKeySaved":
                    handleApiKeySaved(msg.payload);
                    break;
                case "initialState":
                    if (msg.payload.providerStatus) { updateProviderDots(msg.payload.providerStatus); }
                    break;
            }
        });

        // == render results == //
        function renderResults(response) {
            analyzeBtn.disabled = false;
            spinner.classList.remove("visible");
            resultsDiv.classList.add("visible");

            // metadata
            const meta = response.metadata;
            resultsMeta.textContent = "Model: " + meta.modelUsed +
                " | Tokens: " + meta.tokensUsed +
                " | Latency: " + meta.latencyMs + "ms";

            // assumptions
            assumptionsList.innerHTML = "";
            for (const a of response.assumptions) {
                const card = document.createElement("div");
                card.className = "assumption-card " + a.riskLevel;
                card.innerHTML =
                    '<div class="card-header">' +
                        '<span class="risk-badge ' + a.riskLevel + '">' + a.riskLevel + '</span>' +
                        '<span class="category-badge">' + formatCategory(a.category) + '</span>' +
                        '<span class="confidence">' + Math.round(a.confidence * 100) + '%</span>' +
                    '</div>' +
                    '<div class="assumption-text">' + escapeHtml(a.assumptionText) + '</div>' +
                    '<div class="rationale">' + escapeHtml(a.rationale) + '</div>' +
                    (a.clarifyingQuestion
                        ? '<div class="clarifying">' + escapeHtml(a.clarifyingQuestion) + '</div>'
                        : "");
                assumptionsList.appendChild(card);
            }

            // suggested prompts
            if (response.suggestedPrompts && response.suggestedPrompts.length > 0) {
                suggestedSection.style.display = "block";
                suggestedList.innerHTML = "";
                for (const sp of response.suggestedPrompts) {
                    const div = document.createElement("div");
                    div.className = "suggested-prompt";
                    div.textContent = sp;
                    div.addEventListener("click", () => {
                        promptInput.value = sp;
                        vscode.setState({ ...vscode.getState(), prompt: sp });
                    });
                    suggestedList.appendChild(div);
                }
            } else {
                suggestedSection.style.display = "none";
            }
        }

        // == render error == //
        function renderError(errorMsg) {
            analyzeBtn.disabled = false;
            spinner.classList.remove("visible");
            resultsDiv.classList.add("visible");
            resultsMeta.textContent = "";
            assumptionsList.innerHTML =
                '<div style="color: var(--vscode-errorForeground);">' +
                escapeHtml(errorMsg) + '</div>';
            suggestedSection.style.display = "none";
        }

        // == update provider status dots == //
        function updateProviderDots(status) {
            openaiDot.className    = "status-dot " + (status.openai    ? "configured" : "missing");
            anthropicDot.className = "status-dot " + (status.anthropic ? "configured" : "missing");
        }

        // == handle API key saved confirmation == //
        function handleApiKeySaved(result) {
            if (result.success) {
                settingsMsg.textContent = result.provider + " API key saved.";
                settingsMsg.style.color = "var(--vscode-testing-iconPassed)";
                // clear the input
                if (result.provider === "openai")    { openaiKeyInput.value = ""; }
                if (result.provider === "anthropic") { anthropicKeyInput.value = ""; }
                // refresh provider dots
                vscode.postMessage({ command: "getInitialState" });
            } else {
                settingsMsg.textContent = "Failed to save " + result.provider + " key: " + (result.message || "Unknown error");
                settingsMsg.style.color = "var(--vscode-errorForeground)";
            }
        }

        // == format category for display == //
        function formatCategory(cat) {
            const map = {
                userContext: "User Context",
                domainContext: "Domain",
                constraints: "Constraints",
                outputFormat: "Output Format",
                ambiguity: "Ambiguity",
                other: "Other",
            };
            return map[cat] || cat;
        }

        // == escape HTML to prevent XSS == //
        function escapeHtml(text) {
            const div = document.createElement("div");
            div.appendChild(document.createTextNode(text));
            return div.innerHTML;
        }
    </script>
</body>
</html>`;
}
