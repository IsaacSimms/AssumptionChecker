// <summary>
// Tests for StatusBarManager health polling with stubbed EngineClient.
// Covers: connected/disconnected states, polling, dispose, state transitions.
// </summary>

import * as assert from "assert";
import * as sinon from "sinon";
import { sharedMock } from "./mocks/vscodeSetup"; // registers vscode mock
import { StatusBarManager } from "../statusBar";
import { MockStatusBarItem } from "./mocks/vscode";

describe("StatusBarManager", () => {
    let mockEngine: { checkHealth: sinon.SinonStub };
    let clock: sinon.SinonFakeTimers;

    beforeEach(() => {
        mockEngine = { checkHealth: sinon.stub() };
        clock = sinon.useFakeTimers();
    });

    afterEach(() => {
        clock.restore();
        sinon.restore();
    });

    // == helper to get the last created status bar item == //
    function getItem(): MockStatusBarItem {
        return sharedMock._lastStatusBarItem!;
    }

    // == shows Connected when health check succeeds == //
    it("shows Connected when engine is healthy", async () => {
        mockEngine.checkHealth.resolves(true);
        const manager = new StatusBarManager(mockEngine as any);

        await manager.startPolling();

        assert.ok(getItem().text.includes("Connected"));
        assert.ok(getItem().shown);
        manager.dispose();
    });

    // == shows Disconnected when health check fails == //
    it("shows Disconnected after all rapid polls fail", async () => {
        mockEngine.checkHealth.resolves(false);
        const manager = new StatusBarManager(mockEngine as any);

        const pollPromise = manager.startPolling();

        // advance through all 8 rapid poll attempts (8 * 2s = 16s)
        for (let i = 0; i < 8; i++) {
            await clock.tickAsync(2000);
        }

        assert.ok(getItem().text.includes("Disconnected"));
        manager.dispose();
    });

    // == shows Connecting initially == //
    it("shows Connecting on creation", () => {
        const manager = new StatusBarManager(mockEngine as any);

        assert.ok(getItem().text.includes("Connecting"));
        manager.dispose();
    });

    // == dispose stops polling == //
    it("stops polling after dispose", async () => {
        mockEngine.checkHealth.resolves(true);
        const manager = new StatusBarManager(mockEngine as any);
        await manager.startPolling();

        const callCountBefore = mockEngine.checkHealth.callCount;
        manager.dispose();

        // advance 60s — should not trigger more calls
        await clock.tickAsync(60000);
        assert.strictEqual(mockEngine.checkHealth.callCount, callCountBefore);
    });

    // == transitions from disconnected to connected == //
    it("transitions from disconnected to connected on recovery", async () => {
        mockEngine.checkHealth.resolves(false);
        const manager = new StatusBarManager(mockEngine as any);

        const pollPromise = manager.startPolling();

        // exhaust rapid polls
        for (let i = 0; i < 8; i++) {
            await clock.tickAsync(2000);
        }

        assert.ok(getItem().text.includes("Disconnected"));

        // now engine recovers
        mockEngine.checkHealth.resolves(true);

        // advance to next background poll (30s)
        await clock.tickAsync(30000);

        assert.ok(getItem().text.includes("Connected"));
        manager.dispose();
    });
});
