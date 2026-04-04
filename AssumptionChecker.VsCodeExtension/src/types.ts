// <summary>
// TypeScript DTO interfaces mirroring AssumptionChecker.Contracts.
// All property names use camelCase to match the Engine's JSON serialization.
// Also exports the AVAILABLE_MODELS list and DEFAULT_MODEL constant.
// </summary>

// == AnalyzeRequest — mirrors Contracts/AnalyzeRequest.cs == //
export interface AnalyzeRequest {
    prompt: string;
    template: string;
    maxAssumptions: number;
    model: string;
    fileContexts: FileContext[];
}

// == FileContext — mirrors Contracts/FileContext == //
export interface FileContext {
    filePath: string;
    content: string;
}

// == AnalyzeResponse — mirrors Contracts/AnalyzeResponse.cs == //
export interface AnalyzeResponse {
    assumptions: Assumption[];
    metadata: ResponseMetadata;
    suggestedPrompts: string[];
}

// == Assumption — mirrors Contracts/Assumption.cs == //
export interface Assumption {
    id: string;
    assumptionText: string;
    category: AssumptionCategory;
    riskLevel: RiskLevel;
    clarifyingQuestion: string | null;
    rationale: string;
    confidence: number;
}

// == ResponseMetadata — mirrors Contracts/ResponseMetadata.cs == //
export interface ResponseMetadata {
    modelUsed: string;
    tokensUsed: number;
    latencyMs: number;
}

// == Enums — mirrors Contracts/Enums.cs (camelCase serialized) == //
export type AssumptionCategory =
    | "userContext"
    | "domainContext"
    | "constraints"
    | "outputFormat"
    | "ambiguity"
    | "other";

export type RiskLevel = "low" | "medium" | "high";

// == ProviderStatus — response from GET /settings/providers == //
export interface ProviderStatus {
    openai: boolean;
    anthropic: boolean;
}

// == ApiKeySaveResponse — response from POST /settings/apikey == //
export interface ApiKeySaveResponse {
    saved: boolean;
    provider: string;
}

// == Available models — superset from VsExtension + WPF == //
export const AVAILABLE_MODELS: string[] = [
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

export const DEFAULT_MODEL = "claude-haiku-4-5";

// == Webview message protocol == //
export interface WebviewMessage {
    command: string;
    payload?: any;
}
