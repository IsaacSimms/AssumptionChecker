// <summary>
// HTTP client for all AssumptionChecker Engine endpoints.
// Uses Node's global fetch with AbortController for timeouts.
// Endpoints: /health, /analyze, /settings/apikey, /settings/providers
// </summary>

import {
    AnalyzeRequest,
    AnalyzeResponse,
    ApiKeySaveResponse,
    ProviderStatus,
} from "./types";

// == EngineClient == //
export class EngineClient {
    private readonly baseUrl: string;

    constructor(baseUrl: string) {
        this.baseUrl = baseUrl.replace(/\/+$/, ""); // strip trailing slashes
    }

    // == checkHealth — GET /health == //
    async checkHealth(): Promise<boolean> {
        try {
            const response = await this.fetchWithTimeout(`${this.baseUrl}/health`, { method: "GET" }, 5000);
            if (!response.ok) { return false; }
            const data = await response.json() as { status: string };
            return data.status === "healthy";
        } catch {
            return false;
        }
    }

    // == getProviders — GET /settings/providers == //
    async getProviders(): Promise<ProviderStatus> {
        const response = await this.fetchWithTimeout(`${this.baseUrl}/settings/providers`, { method: "GET" }, 5000);
        if (!response.ok) {
            throw new Error(`Engine returned ${response.status} from /settings/providers`);
        }
        return (await response.json()) as ProviderStatus;
    }

    // == saveApiKey — POST /settings/apikey == //
    async saveApiKey(provider: string, apiKey: string): Promise<ApiKeySaveResponse> {
        const response = await this.fetchWithTimeout(
            `${this.baseUrl}/settings/apikey`,
            {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ provider, apiKey }),
            },
            5000,
        );
        if (!response.ok) {
            throw new Error(`Engine returned ${response.status} from /settings/apikey`);
        }
        return (await response.json()) as ApiKeySaveResponse;
    }

    // == analyze — POST /analyze == //
    async analyze(request: AnalyzeRequest): Promise<AnalyzeResponse> {
        const response = await this.fetchWithTimeout(
            `${this.baseUrl}/analyze`,
            {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(request),
            },
            60000, // LLM calls can be slow
        );
        if (!response.ok) {
            const errorText = await response.text().catch(() => "Unknown error");
            throw new Error(`Engine returned ${response.status}: ${errorText}`);
        }
        return (await response.json()) as AnalyzeResponse;
    }

    // == fetchWithTimeout — wraps fetch with AbortController == //
    private async fetchWithTimeout(
        url: string,
        options: RequestInit,
        timeoutMs: number,
    ): Promise<Response> {
        const controller = new AbortController();
        const timer = setTimeout(() => controller.abort(), timeoutMs);
        try {
            return await fetch(url, { ...options, signal: controller.signal });
        } finally {
            clearTimeout(timer);
        }
    }
}
