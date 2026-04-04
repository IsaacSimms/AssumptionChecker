"use strict";
// <summary>
// Tests for DTO constants: AVAILABLE_MODELS list and DEFAULT_MODEL value.
// Validates completeness and consistency with Engine defaults.
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
const types_1 = require("../types");
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
        assert.strictEqual(types_1.AVAILABLE_MODELS.length, expectedModels.length);
        for (const model of expectedModels) {
            assert.ok(types_1.AVAILABLE_MODELS.includes(model), `Missing model: ${model}`);
        }
    });
    // == DEFAULT_MODEL should be in the available list == //
    it("should have DEFAULT_MODEL in AVAILABLE_MODELS", () => {
        assert.ok(types_1.AVAILABLE_MODELS.includes(types_1.DEFAULT_MODEL), `DEFAULT_MODEL "${types_1.DEFAULT_MODEL}" not in AVAILABLE_MODELS`);
    });
    // == DEFAULT_MODEL should match Engine default (claude-haiku-4-5) == //
    it("should default to claude-haiku-4-5 matching Engine AnalyzeRequest", () => {
        assert.strictEqual(types_1.DEFAULT_MODEL, "claude-haiku-4-5");
    });
});
//# sourceMappingURL=types.test.js.map