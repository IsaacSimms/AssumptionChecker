// <summary>
// VS Code extension entry point for AssumptionChecker.
// Wires EngineClient, EngineProcessManager, SecretsManager,
// StatusBarManager, SidebarProvider.
// Reads engineUrl from configuration, registers disposables.
// </summary>

import * as vscode from "vscode";
import { EngineClient } from "./engineClient";
import { EngineProcessManager } from "./engineProcess";
import { SecretsManager } from "./secretsManager";
import { StatusBarManager } from "./statusBar";
import { SidebarProvider } from "./sidebarProvider";

// == activate == //
export function activate(context: vscode.ExtensionContext): void {
    const config = vscode.workspace.getConfiguration("assumptionChecker");
    const engineUrl = config.get<string>("engineUrl", "http://localhost:5046");

    // == create core services == //
    const engineClient = new EngineClient(engineUrl);
    const engineProcess = new EngineProcessManager(engineClient);
    const secretsManager = new SecretsManager(context.secrets, engineClient);
    const statusBar = new StatusBarManager(engineClient);
    const sidebarProvider = new SidebarProvider(context.extensionUri, engineClient, secretsManager);

    // == auto-start engine, then begin health polling == //
    engineProcess.ensureRunning().then(() => statusBar.startPolling());

    // == register sidebar webview provider == //
    context.subscriptions.push(
        vscode.window.registerWebviewViewProvider(SidebarProvider.viewId, sidebarProvider),
    );

    // == register disposables == //
    context.subscriptions.push(statusBar);
    context.subscriptions.push(engineProcess);
}

// == deactivate == //
export function deactivate(): void {
    // disposables handle cleanup via context.subscriptions
}
