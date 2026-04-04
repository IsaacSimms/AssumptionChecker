// <summary>
// WebviewViewProvider for the AssumptionChecker sidebar panel.
// Handles message dispatch between webview and extension:
//   analyze, saveApiKey, getInitialState
// Generates webview HTML via sidebarHtml.ts.
// </summary>

import * as vscode from "vscode";
import * as crypto from "crypto";
import { EngineClient } from "./engineClient";
import { SecretsManager } from "./secretsManager";
import { gatherFileContexts } from "./fileContextGatherer";
import { getSidebarHtml } from "./webview/sidebarHtml";
import { AVAILABLE_MODELS, DEFAULT_MODEL, AnalyzeRequest, WebviewMessage } from "./types";

// == SidebarProvider == //
export class SidebarProvider implements vscode.WebviewViewProvider {
    public static readonly viewId = "assumptionChecker.sidebar";

    private view?: vscode.WebviewView;
    private readonly extensionUri: vscode.Uri;
    private readonly engine: EngineClient;
    private readonly secrets: SecretsManager;

    constructor(extensionUri: vscode.Uri, engine: EngineClient, secrets: SecretsManager) {
        this.extensionUri = extensionUri;
        this.engine = engine;
        this.secrets = secrets;
    }

    // == resolveWebviewView — called by VS Code when the sidebar opens == //
    resolveWebviewView(webviewView: vscode.WebviewView): void {
        this.view = webviewView;
        webviewView.webview.options = { enableScripts: true };

        const nonce = crypto.randomBytes(16).toString("hex");
        webviewView.webview.html = getSidebarHtml(nonce, AVAILABLE_MODELS, DEFAULT_MODEL);

        // == message handler == //
        webviewView.webview.onDidReceiveMessage(
            (msg: WebviewMessage) => this.handleMessage(msg),
        );
    }

    // == handleMessage — dispatch incoming webview messages == //
    private async handleMessage(msg: WebviewMessage): Promise<void> {
        switch (msg.command) {
            case "analyze":
                await this.handleAnalyze(msg.payload);
                break;
            case "saveApiKey":
                await this.handleSaveApiKey(msg.payload);
                break;
            case "getInitialState":
                await this.handleGetInitialState();
                break;
        }
    }

    // == handleAnalyze — gather file contexts, call Engine, post result == //
    private async handleAnalyze(payload: { prompt: string; model: string; maxAssumptions: number }): Promise<void> {
        try {
            const fileContexts = gatherFileContexts();

            const request: AnalyzeRequest = {
                prompt: payload.prompt,
                model: payload.model,
                maxAssumptions: payload.maxAssumptions,
                template: "default",
                fileContexts,
            };

            const response = await this.engine.analyze(request);
            this.postMessage({ command: "analyzeResult", payload: response });
        } catch (err: any) {
            const message = err?.message || "Analysis failed. Is the Engine running?";
            this.postMessage({ command: "analyzeError", payload: message });
        }
    }

    // == handleSaveApiKey — persist key + forward to Engine == //
    private async handleSaveApiKey(payload: { provider: "openai" | "anthropic"; apiKey: string }): Promise<void> {
        try {
            await this.secrets.saveApiKey(payload.provider, payload.apiKey);
            this.postMessage({
                command: "apiKeySaved",
                payload: { provider: payload.provider, success: true },
            });
        } catch (err: any) {
            this.postMessage({
                command: "apiKeySaved",
                payload: {
                    provider: payload.provider,
                    success: false,
                    message: err?.message || "Unknown error",
                },
            });
        }
    }

    // == handleGetInitialState — send provider status to webview == //
    private async handleGetInitialState(): Promise<void> {
        let providerStatus = { openai: false, anthropic: false };
        try {
            providerStatus = await this.engine.getProviders();
        } catch {
            // Engine may be down; check local secrets as fallback
            providerStatus = {
                openai: await this.secrets.hasApiKey("openai"),
                anthropic: await this.secrets.hasApiKey("anthropic"),
            };
        }
        this.postMessage({
            command: "initialState",
            payload: { providerStatus, defaultModel: DEFAULT_MODEL },
        });
    }

    // == postMessage — send message to webview == //
    private postMessage(msg: WebviewMessage): void {
        this.view?.webview.postMessage(msg);
    }
}
