// <summary>
// Tests for SecretsManager with in-memory SecretStorage mock and stubbed EngineClient.
// Covers: save, retrieve, missing keys, Engine forward failure, key naming.
// </summary>

import * as assert from "assert";
import * as sinon from "sinon";
import { SecretsManager } from "../secretsManager";
import { createMockSecretStorage } from "./mocks/vscode";

describe("SecretsManager", () => {
    let mockSecrets: ReturnType<typeof createMockSecretStorage>;
    let mockEngine: { saveApiKey: sinon.SinonStub };
    let manager: SecretsManager;

    beforeEach(() => {
        mockSecrets = createMockSecretStorage();
        mockEngine = {
            saveApiKey: sinon.stub().resolves({ saved: true, provider: "openai" }),
        };
        manager = new SecretsManager(mockSecrets as any, mockEngine as any);
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
