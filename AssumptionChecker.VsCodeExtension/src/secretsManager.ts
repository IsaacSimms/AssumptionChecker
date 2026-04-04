// <summary>
// Wraps VS Code SecretStorage for cross-platform API key persistence.
// On save, stores locally AND forwards to Engine via POST /settings/apikey.
// Keys: assumptionChecker.openai.apiKey, assumptionChecker.anthropic.apiKey
// </summary>

import * as vscode from "vscode";
import { EngineClient } from "./engineClient";

export type Provider = "openai" | "anthropic";

const SECRET_KEY_PREFIX = "assumptionChecker";

// == SecretsManager == //
export class SecretsManager {
    private readonly secrets: vscode.SecretStorage;
    private readonly engine: EngineClient;

    constructor(secrets: vscode.SecretStorage, engine: EngineClient) {
        this.secrets = secrets;
        this.engine = engine;
    }

    // == getApiKey — retrieve from SecretStorage == //
    async getApiKey(provider: Provider): Promise<string | undefined> {
        return this.secrets.get(this.keyFor(provider));
    }

    // == hasApiKey — check if a key exists == //
    async hasApiKey(provider: Provider): Promise<boolean> {
        const key = await this.getApiKey(provider);
        return key !== undefined && key.length > 0;
    }

    // == saveApiKey — store locally + forward to Engine == //
    async saveApiKey(provider: Provider, apiKey: string): Promise<void> {
        // always persist locally first
        await this.secrets.store(this.keyFor(provider), apiKey);

        // forward to Engine for hot-reload (best-effort)
        try {
            await this.engine.saveApiKey(provider, apiKey);
        } catch {
            // Engine may be down; key is still saved locally
        }
    }

    // == deleteApiKey — remove from SecretStorage == //
    async deleteApiKey(provider: Provider): Promise<void> {
        await this.secrets.delete(this.keyFor(provider));
    }

    // == keyFor — builds the secret storage key name == //
    private keyFor(provider: Provider): string {
        return `${SECRET_KEY_PREFIX}.${provider}.apiKey`;
    }
}
