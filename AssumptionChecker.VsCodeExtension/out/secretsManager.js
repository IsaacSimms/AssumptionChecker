"use strict";
// <summary>
// Wraps VS Code SecretStorage for cross-platform API key persistence.
// On save, stores locally AND forwards to Engine via POST /settings/apikey.
// Keys: assumptionChecker.openai.apiKey, assumptionChecker.anthropic.apiKey
// </summary>
Object.defineProperty(exports, "__esModule", { value: true });
exports.SecretsManager = void 0;
const SECRET_KEY_PREFIX = "assumptionChecker";
// == SecretsManager == //
class SecretsManager {
    secrets;
    engine;
    constructor(secrets, engine) {
        this.secrets = secrets;
        this.engine = engine;
    }
    // == getApiKey — retrieve from SecretStorage == //
    async getApiKey(provider) {
        return this.secrets.get(this.keyFor(provider));
    }
    // == hasApiKey — check if a key exists == //
    async hasApiKey(provider) {
        const key = await this.getApiKey(provider);
        return key !== undefined && key.length > 0;
    }
    // == saveApiKey — store locally + forward to Engine == //
    async saveApiKey(provider, apiKey) {
        // always persist locally first
        await this.secrets.store(this.keyFor(provider), apiKey);
        // forward to Engine for hot-reload (best-effort)
        try {
            await this.engine.saveApiKey(provider, apiKey);
        }
        catch {
            // Engine may be down; key is still saved locally
        }
    }
    // == deleteApiKey — remove from SecretStorage == //
    async deleteApiKey(provider) {
        await this.secrets.delete(this.keyFor(provider));
    }
    // == keyFor — builds the secret storage key name == //
    keyFor(provider) {
        return `${SECRET_KEY_PREFIX}.${provider}.apiKey`;
    }
}
exports.SecretsManager = SecretsManager;
//# sourceMappingURL=secretsManager.js.map