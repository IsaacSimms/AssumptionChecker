"use strict";
// <summary>
// Tests for SidebarProvider message dispatch with mocked webview and dependencies.
// Covers: analyze, saveApiKey, getInitialState messages.
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
const assert = __importStar(require("assert"));
const sinon = __importStar(require("sinon"));
const vscodeSetup_1 = require("./mocks/vscodeSetup"); // registers vscode mock
const sidebarProvider_1 = require("../sidebarProvider");
const vscode_1 = require("./mocks/vscode");
describe("SidebarProvider", () => {
    let provider;
    let mockEngine;
    let mockSecretsManager;
    let mockView;
    beforeEach(() => {
        vscodeSetup_1.sharedMock.workspace.textDocuments = [];
        mockEngine = {
            analyze: sinon.stub().resolves({
                assumptions: [{ id: "a1", assumptionText: "Test", category: "other", riskLevel: "low", clarifyingQuestion: null, rationale: "R", confidence: 0.8 }],
                metadata: { modelUsed: "test-model", tokensUsed: 100, latencyMs: 500 },
                suggestedPrompts: ["Better prompt"],
            }),
            saveApiKey: sinon.stub().resolves({ saved: true, provider: "openai" }),
            getProviders: sinon.stub().resolves({ openai: true, anthropic: false }),
        };
        mockSecretsManager = {
            saveApiKey: sinon.stub().resolves(),
            getApiKey: sinon.stub().resolves(undefined),
            hasApiKey: sinon.stub().resolves(false),
        };
        provider = new sidebarProvider_1.SidebarProvider({ scheme: "file", fsPath: "/ext" }, mockEngine, mockSecretsManager);
        mockView = (0, vscode_1.createMockWebviewView)();
        provider.resolveWebviewView(mockView.webviewView);
    });
    afterEach(() => {
        vscodeSetup_1.sharedMock.workspace.textDocuments = [];
        sinon.restore();
    });
    // == analyze message calls engineClient.analyze() == //
    it("analyze message calls engine with correct args", async () => {
        mockView.fireMessage({
            command: "analyze",
            payload: { prompt: "Test prompt", model: "gpt-4o", maxAssumptions: 5 },
        });
        await new Promise(resolve => setTimeout(resolve, 50));
        assert.ok(mockEngine.analyze.calledOnce);
        const request = mockEngine.analyze.firstCall.args[0];
        assert.strictEqual(request.prompt, "Test prompt");
        assert.strictEqual(request.model, "gpt-4o");
        assert.strictEqual(request.maxAssumptions, 5);
        assert.strictEqual(request.template, "default");
    });
    // == analyze message attaches file contexts == //
    it("analyze message includes file contexts from open editors", async () => {
        vscodeSetup_1.sharedMock.workspace.textDocuments = [
            { uri: { scheme: "file", fsPath: "/test.ts" }, getText: () => "const x = 1;" },
        ];
        mockView.fireMessage({
            command: "analyze",
            payload: { prompt: "Analyze this", model: "claude-haiku-4-5", maxAssumptions: 10 },
        });
        await new Promise(resolve => setTimeout(resolve, 50));
        const request = mockEngine.analyze.firstCall.args[0];
        assert.strictEqual(request.fileContexts.length, 1);
        assert.strictEqual(request.fileContexts[0].filePath, "/test.ts");
    });
    // == analyze message posts result back to webview == //
    it("posts analyzeResult back to webview on success", async () => {
        mockView.fireMessage({
            command: "analyze",
            payload: { prompt: "Test", model: "gpt-4o", maxAssumptions: 10 },
        });
        await new Promise(resolve => setTimeout(resolve, 50));
        const resultMsg = mockView.messages.find((m) => m.command === "analyzeResult");
        assert.ok(resultMsg, "Should have posted analyzeResult");
        assert.strictEqual(resultMsg.payload.assumptions.length, 1);
    });
    // == analyze message posts error on failure == //
    it("posts analyzeError on engine failure", async () => {
        mockEngine.analyze.rejects(new Error("Engine exploded"));
        mockView.fireMessage({
            command: "analyze",
            payload: { prompt: "Test", model: "gpt-4o", maxAssumptions: 10 },
        });
        await new Promise(resolve => setTimeout(resolve, 50));
        const errorMsg = mockView.messages.find((m) => m.command === "analyzeError");
        assert.ok(errorMsg, "Should have posted analyzeError");
        assert.ok(errorMsg.payload.includes("Engine exploded"));
    });
    // == saveApiKey message calls secretsManager == //
    it("saveApiKey message calls secrets manager", async () => {
        mockView.fireMessage({
            command: "saveApiKey",
            payload: { provider: "openai", apiKey: "sk-test" },
        });
        await new Promise(resolve => setTimeout(resolve, 50));
        assert.ok(mockSecretsManager.saveApiKey.calledOnce);
        assert.deepStrictEqual(mockSecretsManager.saveApiKey.firstCall.args, ["openai", "sk-test"]);
    });
    // == getInitialState returns provider status == //
    it("getInitialState returns provider status", async () => {
        mockView.fireMessage({ command: "getInitialState" });
        await new Promise(resolve => setTimeout(resolve, 50));
        const stateMsg = mockView.messages.find((m) => m.command === "initialState");
        assert.ok(stateMsg, "Should have posted initialState");
        assert.strictEqual(stateMsg.payload.providerStatus.openai, true);
        assert.strictEqual(stateMsg.payload.providerStatus.anthropic, false);
    });
});
//# sourceMappingURL=sidebarProvider.test.js.map