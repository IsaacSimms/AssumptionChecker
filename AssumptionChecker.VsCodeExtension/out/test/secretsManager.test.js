"use strict";
// <summary>
// Tests for SecretsManager with in-memory SecretStorage mock and stubbed EngineClient.
// Covers: save, retrieve, missing keys, Engine forward failure, key naming.
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
const secretsManager_1 = require("../secretsManager");
const vscode_1 = require("./mocks/vscode");
describe("SecretsManager", () => {
    let mockSecrets;
    let mockEngine;
    let manager;
    beforeEach(() => {
        mockSecrets = (0, vscode_1.createMockSecretStorage)();
        mockEngine = {
            saveApiKey: sinon.stub().resolves({ saved: true, provider: "openai" }),
        };
        manager = new secretsManager_1.SecretsManager(mockSecrets, mockEngine);
    });
    afterEach(() => {
        sinon.restore();
    });
    // == saveApiKey stores in SecretStorage == //
    it("stores API key in SecretStorage", async () => {
        await manager.saveApiKey("openai", "sk-test-123");
        const stored = await mockSecrets.get("assumptionChecker.openai.apiKey");
        assert.strictEqual(stored, "sk-test-123");
    });
    // == saveApiKey forwards to Engine == //
    it("forwards API key to Engine", async () => {
        await manager.saveApiKey("anthropic", "sk-ant-test");
        assert.ok(mockEngine.saveApiKey.calledOnce);
        assert.deepStrictEqual(mockEngine.saveApiKey.firstCall.args, ["anthropic", "sk-ant-test"]);
    });
    // == getApiKey retrieves from SecretStorage == //
    it("retrieves stored API key", async () => {
        await mockSecrets.store("assumptionChecker.openai.apiKey", "sk-stored");
        const result = await manager.getApiKey("openai");
        assert.strictEqual(result, "sk-stored");
    });
    // == getApiKey returns undefined when not set == //
    it("returns undefined for missing key", async () => {
        const result = await manager.getApiKey("anthropic");
        assert.strictEqual(result, undefined);
    });
    // == saveApiKey still saves locally if Engine forward fails == //
    it("saves locally even when Engine forward fails", async () => {
        mockEngine.saveApiKey.rejects(new Error("Engine down"));
        await manager.saveApiKey("openai", "sk-local-only");
        const stored = await mockSecrets.get("assumptionChecker.openai.apiKey");
        assert.strictEqual(stored, "sk-local-only");
    });
    // == uses correct key names for each provider == //
    it("uses correct SecretStorage key names", async () => {
        await manager.saveApiKey("openai", "key1");
        await manager.saveApiKey("anthropic", "key2");
        assert.strictEqual(await mockSecrets.get("assumptionChecker.openai.apiKey"), "key1");
        assert.strictEqual(await mockSecrets.get("assumptionChecker.anthropic.apiKey"), "key2");
    });
    // == hasApiKey == //
    it("hasApiKey returns true when key exists", async () => {
        await manager.saveApiKey("openai", "sk-exists");
        const result = await manager.hasApiKey("openai");
        assert.strictEqual(result, true);
    });
    it("hasApiKey returns false when key is missing", async () => {
        const result = await manager.hasApiKey("anthropic");
        assert.strictEqual(result, false);
    });
});
//# sourceMappingURL=secretsManager.test.js.map