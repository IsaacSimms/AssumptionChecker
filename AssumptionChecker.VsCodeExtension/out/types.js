"use strict";
// <summary>
// TypeScript DTO interfaces mirroring AssumptionChecker.Contracts.
// All property names use camelCase to match the Engine's JSON serialization.
// Also exports the AVAILABLE_MODELS list and DEFAULT_MODEL constant.
// </summary>
Object.defineProperty(exports, "__esModule", { value: true });
exports.DEFAULT_MODEL = exports.AVAILABLE_MODELS = void 0;
// == Available models — superset from VsExtension + WPF == //
exports.AVAILABLE_MODELS = [
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
exports.DEFAULT_MODEL = "claude-haiku-4-5";
//# sourceMappingURL=types.js.map