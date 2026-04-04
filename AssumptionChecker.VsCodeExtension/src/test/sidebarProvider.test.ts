// <summary>
// Tests for SidebarProvider message dispatch with mocked webview and dependencies.
// Covers: analyze, saveApiKey, getInitialState messages.
// </summary>

import * as assert from "assert";
import * as sinon from "sinon";
import { sharedMock } from "./mocks/vscodeSetup"; // registers vscode mock
import { SidebarProvider } from "../sidebarProvider";
import { createMockWebviewView, createMockSecretStorage } from "./mocks/vscode";

describe("SidebarProvider", () => {
    let provider: SidebarProvider;
    let mockEngine: {
        analyze: sinon.SinonStub;
        saveApiKey: sinon.SinonStub;
        getProviders: sinon.SinonStub;
    };
    let mockSecretsManager: {
        saveApiKey: sinon.SinonStub;
        getApiKey: sinon.SinonStub;
        hasApiKey: sinon.SinonStub;
    };
    let mockView: ReturnType<typeof createMockWebviewView>;

    beforeEach(() => {
        sharedMock.workspace.textDocuments = [];

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

        provider = new SidebarProvider(
            { scheme: "file", fsPath: "/ext" } as any,
            mockEngine as any,
            mockSecretsManager as any,
        );

        mockView = createMockWebviewView();
        provider.resolveWebviewView(mockView.webviewView as any);
    });

    afterEach(() => {
        sharedMock.workspace.textDocuments = [];
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
        sharedMock.workspace.textDocuments = [
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

        const resultMsg = mockView.messages.find((m: any) => m.command === "analyzeResult");
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

        const errorMsg = mockView.messages.find((m: any) => m.command === "analyzeError");
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

        const stateMsg = mockView.messages.find((m: any) => m.command === "initialState");
        assert.ok(stateMsg, "Should have posted initialState");
        assert.strictEqual(stateMsg.payload.providerStatus.openai, true);
        assert.strictEqual(stateMsg.payload.providerStatus.anthropic, false);
    });
});
