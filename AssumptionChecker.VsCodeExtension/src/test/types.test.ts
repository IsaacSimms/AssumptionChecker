// <summary>
// Tests for DTO constants: AVAILABLE_MODELS list and DEFAULT_MODEL value.
// Validates completeness and consistency with Engine defaults.
// </summary>

import * as assert from "assert";
import { AVAILABLE_MODELS, DEFAULT_MODEL } from "../types";

describe("Types & Constants", () => {
    // == AVAILABLE_MODELS should contain all expected models == //
    it("should contain all 15 expected models", () => {
        const expectedModels = [
            // Anthropic
            "claude-haiku-4-5",
            "claude-sonnet-4-6",
            "claude-opus-4-6",
            // OpenAI
            "gpt-4o-mini",
            "gpt-4o",
            "gpt-4.1",
            "gpt-4.1-mini",
            "gpt-4.1-nano",
            "o1-mini",
            "o1",
            "o3-mini",
            "gpt-5-mini",
            "gpt-5.1",
            "gpt-5.1-Codex",
            "gpt-5.2",
        ];

        assert.strictEqual(AVAILABLE_MODELS.length, expectedModels.length);
        for (const model of expectedModels) {
            assert.ok(
                AVAILABLE_MODELS.includes(model),
                `Missing model: ${model}`,
            );
        }
    });

    // == DEFAULT_MODEL should be in the available list == //
    it("should have DEFAULT_MODEL in AVAILABLE_MODELS", () => {
        assert.ok(
            AVAILABLE_MODELS.includes(DEFAULT_MODEL),
            `DEFAULT_MODEL "${DEFAULT_MODEL}" not in AVAILABLE_MODELS`,
        );
    });

    // == DEFAULT_MODEL should match Engine default (claude-haiku-4-5) == //
    it("should default to claude-haiku-4-5 matching Engine AnalyzeRequest", () => {
        assert.strictEqual(DEFAULT_MODEL, "claude-haiku-4-5");
    });
});
