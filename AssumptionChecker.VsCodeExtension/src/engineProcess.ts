// <summary>
// Manages the Engine child process lifecycle.
// Spawns AssumptionChecker.Engine.exe when the health check fails,
// kills it on dispose. Resolves the engine path from configuration
// or well-known install locations.
// </summary>

import * as vscode from "vscode";
import * as cp from "child_process";
import * as path from "path";
import * as fs from "fs";
import { EngineClient } from "./engineClient";

// == EngineProcessManager == //
export class EngineProcessManager implements vscode.Disposable {
    private readonly engine: EngineClient;
    private process: cp.ChildProcess | undefined;
    private readonly outputChannel: vscode.OutputChannel;

    constructor(engine: EngineClient) {
        this.engine = engine;
        this.outputChannel = vscode.window.createOutputChannel("AssumptionChecker Engine");
    }

    // == ensureRunning — spawn the engine if it's not already reachable == //
    async ensureRunning(): Promise<void> {
        const config = vscode.workspace.getConfiguration("assumptionChecker");
        if (!config.get<boolean>("autoStartEngine", true)) {
            return; // user opted out
        }

        const healthy = await this.engine.checkHealth();
        if (healthy) {
            this.outputChannel.appendLine("[Engine] Already running — skipping launch.");
            return;
        }

        const enginePath = this.resolveEnginePath(config);
        if (!enginePath) {
            this.outputChannel.appendLine("[Engine] Could not locate Engine executable. Start it manually.");
            vscode.window.showWarningMessage(
                "AssumptionChecker: Could not find Engine executable. " +
                "Set `assumptionChecker.enginePath` in settings or start the Engine manually.",
            );
            return;
        }

        this.outputChannel.appendLine(`[Engine] Starting: ${enginePath}`);
        this.process = cp.spawn(enginePath, [], {
            stdio: ["ignore", "pipe", "pipe"],
            detached: false,
            windowsHide: true,
        });

        // == pipe stdout/stderr to output channel == //
        this.process.stdout?.on("data", (data: Buffer) => {
            this.outputChannel.appendLine(data.toString().trimEnd());
        });
        this.process.stderr?.on("data", (data: Buffer) => {
            this.outputChannel.appendLine(`[stderr] ${data.toString().trimEnd()}`);
        });

        this.process.on("exit", (code) => {
            this.outputChannel.appendLine(`[Engine] Process exited with code ${code}`);
            this.process = undefined;
        });

        // == wait for the engine to become healthy == //
        const ready = await this.waitForReady(8000);
        if (ready) {
            this.outputChannel.appendLine("[Engine] Ready.");
        } else {
            this.outputChannel.appendLine("[Engine] Started but health check still failing — it may need more time.");
        }
    }

    // == resolveEnginePath — check config, then well-known locations == //
    private resolveEnginePath(config: vscode.WorkspaceConfiguration): string | undefined {
        // 1. Explicit user setting
        const explicit = config.get<string>("enginePath", "").trim();
        if (explicit && fs.existsSync(explicit)) {
            return explicit;
        }

        // 2. MSI install location
        const localAppData = process.env["LOCALAPPDATA"] ?? "";
        const msiPath = path.join(localAppData, "AssumptionChecker", "Engine", "AssumptionChecker.Engine.exe");
        if (fs.existsSync(msiPath)) {
            return msiPath;
        }

        // 3. Sibling project in workspace (source build)
        const workspaceFolders = vscode.workspace.workspaceFolders;
        if (workspaceFolders) {
            for (const folder of workspaceFolders) {
                const devPath = path.join(
                    folder.uri.fsPath,
                    "AssumptionChecker.Engine", "bin", "Debug", "net8.0", "AssumptionChecker.Engine.exe",
                );
                if (fs.existsSync(devPath)) {
                    return devPath;
                }
                const releasePath = path.join(
                    folder.uri.fsPath,
                    "AssumptionChecker.Engine", "bin", "Release", "net8.0", "AssumptionChecker.Engine.exe",
                );
                if (fs.existsSync(releasePath)) {
                    return releasePath;
                }
            }
        }

        return undefined;
    }

    // == waitForReady — poll /health until it responds == //
    private async waitForReady(maxWaitMs: number): Promise<boolean> {
        const start = Date.now();
        while (Date.now() - start < maxWaitMs) {
            if (await this.engine.checkHealth()) {
                return true;
            }
            await new Promise((r) => setTimeout(r, 500));
        }
        return false;
    }

    // == dispose — kill the child process == //
    dispose(): void {
        if (this.process && !this.process.killed) {
            this.outputChannel.appendLine("[Engine] Stopping...");
            this.process.kill();
            this.process = undefined;
        }
        this.outputChannel.dispose();
    }
}