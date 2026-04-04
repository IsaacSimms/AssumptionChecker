"use strict";
// <summary>
// WebviewViewProvider for the AssumptionChecker sidebar panel.
// Handles message dispatch between webview and extension:
//   analyze, saveApiKey, getInitialState
// Generates webview HTML via sidebarHtml.ts.
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
exports.SidebarProvider = void 0;
const crypto = __importStar(require("crypto"));
const fileContextGatherer_1 = require("./fileContextGatherer");
const sidebarHtml_1 = require("./webview/sidebarHtml");
const types_1 = require("./types");
// == SidebarProvider == //
class SidebarProvider {
    static viewId = "assumptionChecker.sidebar";
    view;
    extensionUri;
    engine;
    secrets;
    constructor(extensionUri, engine, secrets) {
        this.extensionUri = extensionUri;
        this.engine = engine;
        this.secrets = secrets;
    }
    // == resolveWebviewView — called by VS Code when the sidebar opens == //
    resolveWebviewView(webviewView) {
        this.view = webviewView;
        webviewView.webview.options = { enableScripts: true };
        const nonce = crypto.randomBytes(16).toString("hex");
        webviewView.webview.html = (0, sidebarHtml_1.getSidebarHtml)(nonce, types_1.AVAILABLE_MODELS, types_1.DEFAULT_MODEL);
        // == message handler == //
        webviewView.webview.onDidReceiveMessage((msg) => this.handleMessage(msg));
    }
    // == handleMessage — dispatch incoming webview messages == //
    async handleMessage(msg) {
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
    async handleAnalyze(payload) {
        try {
            const fileContexts = (0, fileContextGatherer_1.gatherFileContexts)();
            const request = {
                prompt: payload.prompt,
                model: payload.model,
                maxAssumptions: payload.maxAssumptions,
                template: "default",
                fileContexts,
            };
            const response = await this.engine.analyze(request);
            this.postMessage({ command: "analyzeResult", payload: response });
        }
        catch (err) {
            const message = err?.message || "Analysis failed. Is the Engine running?";
            this.postMessage({ command: "analyzeError", payload: message });
        }
    }
    // == handleSaveApiKey — persist key + forward to Engine == //
    async handleSaveApiKey(payload) {
        try {
            await this.secrets.saveApiKey(payload.provider, payload.apiKey);
            this.postMessage({
                command: "apiKeySaved",
                payload: { provider: payload.provider, success: true },
            });
        }
        catch (err) {
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
    async handleGetInitialState() {
        let providerStatus = { openai: false, anthropic: false };
        try {
            providerStatus = await this.engine.getProviders();
        }
        catch {
            // Engine may be down; check local secrets as fallback
            providerStatus = {
                openai: await this.secrets.hasApiKey("openai"),
                anthropic: await this.secrets.hasApiKey("anthropic"),
            };
        }
        this.postMessage({
            command: "initialState",
            payload: { providerStatus, defaultModel: types_1.DEFAULT_MODEL },
        });
    }
    // == postMessage — send message to webview == //
    postMessage(msg) {
        this.view?.webview.postMessage(msg);
    }
}
exports.SidebarProvider = SidebarProvider;
//# sourceMappingURL=sidebarProvider.js.map