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
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
const assert = __importStar(require("assert"));
const sinon = __importStar(require("sinon"));
// == mock vscode module before imports == //
const module_1 = __importDefault(require("module"));
const mockVscode = {
    workspace: { textDocuments: [] },
    Uri: { file: (p) => ({ scheme: "file", fsPath: p }) },
};
const originalResolveFilename = module_1.default._resolveFilename;
module_1.default._resolveFilename = function (request, ...args) {
    if (request === "vscode") {
        return request;
    }
    return originalResolveFilename.call(this, request, ...args);
};
require.cache["vscode"] = {
    id: "vscode",
    filename: "vscode",
    loaded: true,
    exports: mockVscode,
    parent: null,
    children: [],
    paths: [],
    path: "",
    require: require,
    isPreloading: false,
};
const sidebarProvider_1 = require("../sidebarProvider");
const vscode_1 = require("./mocks/vscode");
describe("SidebarProvider", () => {
    let provider;
    let mockEngine;
    let mockSecrets;
    let mockView;
    beforeEach(() => {
        mockEngine = {
            analyze: sinon.stub().resolves({
                assumptions: [{ id: "a1", assumptionText: "Test", category: "other", riskLevel: "low", clarifyingQuestion: null, rationale: "R", confidence: 0.8 }],
                metadata: { modelUsed: "test-model", tokensUsed: 100, latencyMs: 500 },
                suggestedPrompts: ["Better prompt"],
            }),
            saveApiKey: sinon.stub().resolves({ saved: true, provider: "openai" }),
            getProviders: sinon.stub().resolves({ openai: true, anthropic: false }),
        };
        mockSecrets = (0, vscode_1.createMockSecretStorage)();
        const mockSecretsManager = {
            saveApiKey: sinon.stub().resolves(),
            getApiKey: sinon.stub().resolves(undefined),
            hasApiKey: sinon.stub().resolves(false),
        };
        provider = new sidebarProvider_1.SidebarProvider({ scheme: "file", fsPath: "/ext" }, mockEngine, mockSecretsManager);
        mockView = (0, vscode_1.createMockWebviewView)();
        provider.resolveWebviewView(mockView.webviewView);
    });
    afterEach(() => {
        sinon.restore();
    });
    // == analyze message calls engineClient.analyze() == //
    it("analyze message calls engine with correct args", async () => {
        mockView.fireMessage({
            command: "analyze",
            payload: { prompt: "Test prompt", model: "gpt-4o", maxAssumptions: 5 },
        });
        // wait for async handling
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
        mockVscode.workspace.textDocuments = [
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
        mockVscode.workspace.textDocuments = []; // cleanup
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