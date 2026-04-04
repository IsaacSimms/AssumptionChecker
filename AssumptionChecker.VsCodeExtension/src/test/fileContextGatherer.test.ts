// <summary>
// Tests for gatherFileContexts() with mocked vscode.workspace.textDocuments.
// Covers: empty docs, scheme filtering, truncation, path preservation, multi-file.
// </summary>

import * as assert from "assert";
import { sharedMock } from "./mocks/vscodeSetup"; // registers vscode mock
import { gatherFileContexts } from "../fileContextGatherer";

// == helper to create a mock TextDocument == //
function createDoc(fsPath: string, content: string, scheme = "file"): any {
    return {
        uri: { scheme, fsPath, toString: () => `${scheme}://${fsPath}` },
        getText: () => content,
    };
}

describe("gatherFileContexts", () => {
    beforeEach(() => {
        sharedMock.workspace.textDocuments = [];
    });

    afterEach(() => {
        sharedMock.workspace.textDocuments = [];
    });

    // == returns empty when no documents open == //
    it("returns empty array when no documents are open", () => {
        const result = gatherFileContexts();
        assert.strictEqual(result.length, 0);
    });

    // == filters to file:// scheme only == //
    it("gathers only file:// scheme documents", () => {
        sharedMock.workspace.textDocuments = [
            createDoc("/src/app.ts", "const a = 1;", "file"),
            createDoc("Untitled-1", "draft", "untitled"),
            createDoc("/repo/file.ts", "diff content", "git"),
            createDoc("/src/utils.ts", "export const b = 2;", "file"),
        ];

        const result = gatherFileContexts();

        assert.strictEqual(result.length, 2);
        assert.strictEqual(result[0].filePath, "/src/app.ts");
        assert.strictEqual(result[1].filePath, "/src/utils.ts");
    });

    // == truncates content over 10,000 chars == //
    it("truncates content exceeding 10,000 characters", () => {
        const longContent = "x".repeat(15_000);
        sharedMock.workspace.textDocuments = [createDoc("/big.ts", longContent)];

        const result = gatherFileContexts();

        assert.strictEqual(result.length, 1);
        assert.ok(result[0].content.length < 15_000);
        assert.ok(result[0].content.length > 10_000); // 10000 + truncation marker
        assert.ok(result[0].content.endsWith("// ... (truncated)"));
    });

    // == preserves filePath from uri.fsPath == //
    it("preserves filePath from document uri", () => {
        sharedMock.workspace.textDocuments = [createDoc("/home/user/project/main.ts", "code here")];

        const result = gatherFileContexts();

        assert.strictEqual(result[0].filePath, "/home/user/project/main.ts");
    });

    // == handles multiple documents == //
    it("returns all open file documents", () => {
        sharedMock.workspace.textDocuments = [
            createDoc("/a.ts", "file a"),
            createDoc("/b.ts", "file b"),
            createDoc("/c.ts", "file c"),
        ];

        const result = gatherFileContexts();

        assert.strictEqual(result.length, 3);
        assert.strictEqual(result[0].content, "file a");
        assert.strictEqual(result[1].content, "file b");
        assert.strictEqual(result[2].content, "file c");
    });
});
