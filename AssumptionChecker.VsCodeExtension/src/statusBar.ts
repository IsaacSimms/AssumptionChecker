// <summary>
// Manages a VS Code status bar item showing Engine connection state.
// Polls /health on activation and periodically in the background.
// States: Connected (green), Disconnected (yellow), Connecting (spinner).
// </summary>

import * as vscode from "vscode";
import { EngineClient } from "./engineClient";

// == StatusBarManager == //
export class StatusBarManager implements vscode.Disposable {
    private readonly item: vscode.StatusBarItem;
    private readonly engine: EngineClient;
    private pollingTimer: ReturnType<typeof setInterval> | undefined;
    private disposed = false;

    constructor(engine: EngineClient) {
        this.engine = engine;
        this.item = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Left, 0);
        this.setConnecting();
        this.item.show();
    }

    // == startPolling — initial rapid poll then background interval == //
    async startPolling(): Promise<void> {
        // rapid poll: every 2s for up to 15s
        let attempts = 0;
        const rapidPoll = async () => {
            if (this.disposed) { return; }
            const healthy = await this.engine.checkHealth();
            if (healthy) {
                this.setConnected();
                this.startBackgroundPolling();
                return;
            }
            attempts++;
            if (attempts >= 8) { // 8 * 2s = 16s
                this.setDisconnected();
                this.startBackgroundPolling();
                return;
            }
            setTimeout(() => rapidPoll(), 2000);
        };
        await rapidPoll();
    }

    // == startBackgroundPolling — check every 30s == //
    private startBackgroundPolling(): void {
        if (this.disposed) { return; }
        this.pollingTimer = setInterval(async () => {
            if (this.disposed) { return; }
            const healthy = await this.engine.checkHealth();
            if (healthy) {
                this.setConnected();
            } else {
                this.setDisconnected();
            }
        }, 30000);
    }

    // == status setters == //
    private setConnected(): void {
        this.item.text = "$(check) Engine: Connected";
        this.item.backgroundColor = undefined;
        this.item.tooltip = "AssumptionChecker Engine is running";
    }

    private setDisconnected(): void {
        this.item.text = "$(warning) Engine: Disconnected";
        this.item.backgroundColor = new vscode.ThemeColor("statusBarItem.warningBackground");
        this.item.tooltip = "AssumptionChecker Engine is not running. Start it with: cd AssumptionChecker.Engine && dotnet run";
    }

    private setConnecting(): void {
        this.item.text = "$(sync~spin) Engine: Connecting...";
        this.item.backgroundColor = undefined;
        this.item.tooltip = "Connecting to AssumptionChecker Engine...";
    }

    // == dispose == //
    dispose(): void {
        this.disposed = true;
        if (this.pollingTimer) {
            clearInterval(this.pollingTimer);
            this.pollingTimer = undefined;
        }
        this.item.dispose();
    }
}
