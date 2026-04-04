"use strict";
// <summary>
// VS Code extension entry point for AssumptionChecker.
// Wires EngineClient, SecretsManager, StatusBarManager, SidebarProvider.
// Reads engineUrl from configuration, registers disposables.
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
exports.activate = activate;
exports.deactivate = deactivate;
const vscode = __importStar(require("vscode"));
const engineClient_1 = require("./engineClient");
const secretsManager_1 = require("./secretsManager");
const statusBar_1 = require("./statusBar");
const sidebarProvider_1 = require("./sidebarProvider");
// == activate == //
function activate(context) {
    const config = vscode.workspace.getConfiguration("assumptionChecker");
    const engineUrl = config.get("engineUrl", "http://localhost:5046");
    // == create core services == //
    const engineClient = new engineClient_1.EngineClient(engineUrl);
    const secretsManager = new secretsManager_1.SecretsManager(context.secrets, engineClient);
    const statusBar = new statusBar_1.StatusBarManager(engineClient);
    const sidebarProvider = new sidebarProvider_1.SidebarProvider(context.extensionUri, engineClient, secretsManager);
    // == register sidebar webview provider == //
    context.subscriptions.push(vscode.window.registerWebviewViewProvider(sidebarProvider_1.SidebarProvider.viewId, sidebarProvider));
    // == register status bar + start health polling == //
    context.subscriptions.push(statusBar);
    statusBar.startPolling();
}
// == deactivate == //
function deactivate() {
    // disposables handle cleanup via context.subscriptions
}
//# sourceMappingURL=extension.js.map