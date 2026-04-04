"use strict";
// <summary>
// HTTP client for all AssumptionChecker Engine endpoints.
// Uses Node's global fetch with AbortController for timeouts.
// Endpoints: /health, /analyze, /settings/apikey, /settings/providers
// </summary>
Object.defineProperty(exports, "__esModule", { value: true });
exports.EngineClient = void 0;
// == EngineClient == //
class EngineClient {
    baseUrl;
    constructor(baseUrl) {
        this.baseUrl = baseUrl.replace(/\/+$/, ""); // strip trailing slashes
    }
    // == checkHealth — GET /health == //
    async checkHealth() {
        try {
            const response = await this.fetchWithTimeout(`${this.baseUrl}/health`, { method: "GET" }, 5000);
            if (!response.ok) {
                return false;
            }
            const data = await response.json();
            return data.status === "healthy";
        }
        catch {
            return false;
        }
    }
    // == getProviders — GET /settings/providers == //
    async getProviders() {
        const response = await this.fetchWithTimeout(`${this.baseUrl}/settings/providers`, { method: "GET" }, 5000);
        if (!response.ok) {
            throw new Error(`Engine returned ${response.status} from /settings/providers`);
        }
        return (await response.json());
    }
    // == saveApiKey — POST /settings/apikey == //
    async saveApiKey(provider, apiKey) {
        const response = await this.fetchWithTimeout(`${this.baseUrl}/settings/apikey`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ provider, apiKey }),
        }, 5000);
        if (!response.ok) {
            throw new Error(`Engine returned ${response.status} from /settings/apikey`);
        }
        return (await response.json());
    }
    // == analyze — POST /analyze == //
    async analyze(request) {
        const response = await this.fetchWithTimeout(`${this.baseUrl}/analyze`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(request),
        }, 60000);
        if (!response.ok) {
            const errorText = await response.text().catch(() => "Unknown error");
            throw new Error(`Engine returned ${response.status}: ${errorText}`);
        }
        return (await response.json());
    }
    // == fetchWithTimeout — wraps fetch with AbortController == //
    async fetchWithTimeout(url, options, timeoutMs) {
        const controller = new AbortController();
        const timer = setTimeout(() => controller.abort(), timeoutMs);
        try {
            return await fetch(url, { ...options, signal: controller.signal });
        }
        finally {
            clearTimeout(timer);
        }
    }
}
exports.EngineClient = EngineClient;
//# sourceMappingURL=engineClient.js.map