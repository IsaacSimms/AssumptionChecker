// <summary>
// Tests for EngineClient with Sinon-stubbed global.fetch.
// Covers: health check, analyze, saveApiKey, getProviders, error handling, timeout.
// </summary>

import * as assert from "assert";
import * as sinon from "sinon";
import { EngineClient } from "../engineClient";
import { AnalyzeRequest, AnalyzeResponse } from "../types";

describe("EngineClient", () => {
    let client: EngineClient;
    let fetchStub: sinon.SinonStub;

    beforeEach(() => {
        client = new EngineClient("http://localhost:5046");
        fetchStub = sinon.stub(global, "fetch");
    });

    afterEach(() => {
        sinon.restore();
    });

    // == helper to create a minimal fake Response == //
    function fakeResponse(status: number, body: any): Response {
        return {
            ok: status >= 200 && status < 300,
            status,
            json: async () => body,
            text: async () => JSON.stringify(body),
        } as unknown as Response;
    }

    // == checkHealth == //
    describe("checkHealth()", () => {
        it("returns true on 200 with healthy status", async () => {
            fetchStub.resolves(fakeResponse(200, { status: "healthy" }));
            const result = await client.checkHealth();
            assert.strictEqual(result, true);
        });

        it("returns false on network error", async () => {
            fetchStub.rejects(new Error("ECONNREFUSED"));
            const result = await client.checkHealth();
            assert.strictEqual(result, false);
        });

        it("returns false on non-200 response", async () => {
            fetchStub.resolves(fakeResponse(500, { status: "error" }));
            const result = await client.checkHealth();
            assert.strictEqual(result, false);
        });
    });

    // == analyze == //
    describe("analyze()", () => {
        const sampleRequest: AnalyzeRequest = {
            prompt: "Test prompt",
            model: "claude-haiku-4-5",
            maxAssumptions: 10,
            template: "default",
            fileContexts: [{ filePath: "/test.ts", content: "const x = 1;" }],
        };

        const sampleResponse: AnalyzeResponse = {
            assumptions: [
                {
                    id: "assumption-1",
                    assumptionText: "Test assumption",
                    category: "userContext",
                    riskLevel: "high",
                    clarifyingQuestion: "What context?",
                    rationale: "No context provided",
                    confidence: 0.9,
                },
            ],
            metadata: { modelUsed: "claude-3-5-haiku-20241022", tokensUsed: 500, latencyMs: 1200 },
            suggestedPrompts: ["Improved prompt 1"],
        };

        it("sends correct request body", async () => {
            fetchStub.resolves(fakeResponse(200, sampleResponse));

            await client.analyze(sampleRequest);

            const [url, options] = fetchStub.firstCall.args;
            assert.ok(url.endsWith("/analyze"));
            assert.strictEqual(options.method, "POST");
            assert.strictEqual(options.headers["Content-Type"], "application/json");

            const sentBody = JSON.parse(options.body);
            assert.strictEqual(sentBody.prompt, "Test prompt");
            assert.strictEqual(sentBody.model, "claude-haiku-4-5");
            assert.strictEqual(sentBody.fileContexts.length, 1);
            assert.strictEqual(sentBody.fileContexts[0].filePath, "/test.ts");
        });

        it("deserializes response correctly", async () => {
            fetchStub.resolves(fakeResponse(200, sampleResponse));

            const result = await client.analyze(sampleRequest);

            assert.strictEqual(result.assumptions.length, 1);
            assert.strictEqual(result.assumptions[0].riskLevel, "high");
            assert.strictEqual(result.assumptions[0].confidence, 0.9);
            assert.strictEqual(result.metadata.modelUsed, "claude-3-5-haiku-20241022");
            assert.strictEqual(result.suggestedPrompts.length, 1);
        });

        it("throws on error response", async () => {
            fetchStub.resolves(fakeResponse(400, "Bad request"));

            await assert.rejects(
                () => client.analyze(sampleRequest),
                (err: Error) => err.message.includes("400"),
            );
        });
    });

    // == saveApiKey == //
    describe("saveApiKey()", () => {
        it("posts provider and key correctly", async () => {
            fetchStub.resolves(fakeResponse(200, { saved: true, provider: "openai" }));

            const result = await client.saveApiKey("openai", "sk-test-key");

            const [url, options] = fetchStub.firstCall.args;
            assert.ok(url.endsWith("/settings/apikey"));
            const sentBody = JSON.parse(options.body);
            assert.strictEqual(sentBody.provider, "openai");
            assert.strictEqual(sentBody.apiKey, "sk-test-key");
            assert.strictEqual(result.saved, true);
        });
    });

    // == getProviders == //
    describe("getProviders()", () => {
        it("returns provider status", async () => {
            fetchStub.resolves(fakeResponse(200, { openai: true, anthropic: false }));

            const result = await client.getProviders();

            assert.strictEqual(result.openai, true);
            assert.strictEqual(result.anthropic, false);
        });
    });

    // == timeout == //
    describe("timeout handling", () => {
        it("passes AbortController signal to fetch", async () => {
            fetchStub.resolves(fakeResponse(200, { status: "healthy" }));

            await client.checkHealth();

            const [, options] = fetchStub.firstCall.args;
            assert.ok(options.signal, "fetch should receive an AbortSignal");
            assert.ok(options.signal instanceof AbortSignal, "signal should be an AbortSignal");
        });
    });
});
