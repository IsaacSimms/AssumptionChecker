"use strict";
// <summary>
// Manages a VS Code status bar item showing Engine connection state.
// Polls /health on activation and periodically in the background.
// States: Connected (green), Disconnected (yellow), Connecting (spinner).
// </summary>
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || (function () {
    var ownKeys = function(o) {
        ownKeys = Object.getOwnPropertyNames || function (o) {
            var ar = [];
            for (var k in o) if (Object.prototype.hasOwnProperty.call(o, k)) ar[ar.length] = k;
            return ar;
        };
        return ownKeys(o);
    };
    return function (mod) {
        if (mod && mod.__esModule) return mod;
        var result = {};
        if (mod != null) for (var k = ownKeys(mod), i = 0; i < k.length; i++) if (k[i] !== "default") __createBinding(result, mod, k[i]);
        __setModuleDefault(result, mod);
        return result;
    };
})();
Object.defineProperty(exports, "__esModule", { value: true });
exports.StatusBarManager = void 0;
const vscode = __importStar(require("vscode"));
// == StatusBarManager == //
class StatusBarManager {
    item;
    engine;
    pollingTimer;
    disposed = false;
    constructor(engine) {
        this.engine = engine;
        this.item = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Left, 0);
        this.setConnecting();
        this.item.show();
    }
    // == startPolling — initial rapid poll then background interval == //
    async startPolling() {
        // rapid poll: every 2s for up to 15s
        let attempts = 0;
        const rapidPoll = async () => {
            if (this.disposed) {
                return;
            }
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
    startBackgroundPolling() {
        if (this.disposed) {
            return;
        }
        this.pollingTimer = setInterval(async () => {
            if (this.disposed) {
                return;
            }
            const healthy = await this.engine.checkHealth();
            if (healthy) {
                this.setConnected();
            }
            else {
                this.setDisconnected();
            }
        }, 30000);
    }
    // == status setters == //
    setConnected() {
        this.item.text = "$(check) Engine: Connected";
        this.item.backgroundColor = undefined;
        this.item.tooltip = "AssumptionChecker Engine is running";
    }
    setDisconnected() {
        this.item.text = "$(warning) Engine: Disconnected";
        this.item.backgroundColor = new vscode.ThemeColor("statusBarItem.warningBackground");
        this.item.tooltip = "AssumptionChecker Engine is not running. Start it with: cd AssumptionChecker.Engine && dotnet run";
    }
    setConnecting() {
        this.item.text = "$(sync~spin) Engine: Connecting...";
        this.item.backgroundColor = undefined;
        this.item.tooltip = "Connecting to AssumptionChecker Engine...";
    }
    // == dispose == //
    dispose() {
        this.disposed = true;
        if (this.pollingTimer) {
            clearInterval(this.pollingTimer);
            this.pollingTimer = undefined;
        }
        this.item.dispose();
    }
}
exports.StatusBarManager = StatusBarManager;
//# sourceMappingURL=statusBar.js.map