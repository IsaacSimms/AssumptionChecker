"use strict";
// <summary>
// Tests for StatusBarManager health polling with stubbed EngineClient.
// Covers: connected/disconnected states, polling, dispose, state transitions.
// Uses mocked vscode.window.createStatusBarItem.
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
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
const assert = __importStar(require("assert"));
const sinon = __importStar(require("sinon"));
// == mock vscode module == //
const module_1 = __importDefault(require("module"));
const vscode_1 = require("./mocks/vscode");
let lastCreatedItem;
const mockVscode = {
    window: {
        createStatusBarItem: (alignment, priority) => {
            lastCreatedItem = new vscode_1.MockStatusBarItem();
            lastCreatedItem.alignment = alignment ?? 1;
            lastCreatedItem.priority = priority ?? 0;
            return lastCreatedItem;
        },
    },
    StatusBarAlignment: { Left: 1, Right: 2 },
    ThemeColor: vscode_1.ThemeColor,
};
const originalResolveFilename = module_1.default._resolveFilename;
module_1.default._resolveFilename = function (request, ...args) {
    if (request === "vscode") {
        return request;
    }
    return originalResolveFilename.call(this, request, ...args);
};
require.cache["vscode"] = {
    id: "vscode",
    filename: "vscode",
    loaded: true,
    exports: mockVscode,
    parent: null,
    children: [],
    paths: [],
    path: "",
    require: require,
    isPreloading: false,
};
const statusBar_1 = require("../statusBar");
describe("StatusBarManager", () => {
    let mockEngine;
    let clock;
    beforeEach(() => {
        mockEngine = { checkHealth: sinon.stub() };
        clock = sinon.useFakeTimers();
    });
    afterEach(() => {
        clock.restore();
        sinon.restore();
    });
    // == shows Connected when health check succeeds == //
    it("shows Connected when engine is healthy", async () => {
        mockEngine.checkHealth.resolves(true);
        const manager = new statusBar_1.StatusBarManager(mockEngine);
        await manager.startPolling();
        assert.ok(lastCreatedItem.text.includes("Connected"));
        assert.ok(lastCreatedItem.shown);
        manager.dispose();
    });
    // == shows Disconnected when health check fails == //
    it("shows Disconnected after all rapid polls fail", async () => {
        mockEngine.checkHealth.resolves(false);
        const manager = new statusBar_1.StatusBarManager(mockEngine);
        const pollPromise = manager.startPolling();
        // advance through all 8 rapid poll attempts (8 * 2s = 16s)
        for (let i = 0; i < 8; i++) {
            await clock.tickAsync(2000);
        }
        assert.ok(lastCreatedItem.text.includes("Disconnected"));
        manager.dispose();
    });
    // == shows Connecting initially == //
    it("shows Connecting on creation", () => {
        const manager = new statusBar_1.StatusBarManager(mockEngine);
        assert.ok(lastCreatedItem.text.includes("Connecting"));
        manager.dispose();
    });
    // == dispose stops polling == //
    it("stops polling after dispose", async () => {
        mockEngine.checkHealth.resolves(true);
        const manager = new statusBar_1.StatusBarManager(mockEngine);
        await manager.startPolling();
        const callCountBefore = mockEngine.checkHealth.callCount;
        manager.dispose();
        // advance 60s — should not trigger more calls
        await clock.tickAsync(60000);
        assert.strictEqual(mockEngine.checkHealth.callCount, callCountBefore);
    });
    // == transitions from disconnected to connected == //
    it("transitions from disconnected to connected on recovery", async () => {
        // first fail, then succeed
        mockEngine.checkHealth.resolves(false);
        const manager = new statusBar_1.StatusBarManager(mockEngine);
        const pollPromise = manager.startPolling();
        // exhaust rapid polls
        for (let i = 0; i < 8; i++) {
            await clock.tickAsync(2000);
        }
        assert.ok(lastCreatedItem.text.includes("Disconnected"));
        // now engine recovers
        mockEngine.checkHealth.resolves(true);
        // advance to next background poll (30s)
        await clock.tickAsync(30000);
        assert.ok(lastCreatedItem.text.includes("Connected"));
        manager.dispose();
    });
});
//# sourceMappingURL=statusBar.test.js.map