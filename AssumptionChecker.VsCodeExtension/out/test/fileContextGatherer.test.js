"use strict";
// <summary>
// Tests for gatherFileContexts() with mocked vscode.workspace.textDocuments.
// Covers: empty docs, scheme filtering, truncation, path preservation, multi-file.
// Uses module-level mock injection for the vscode module.
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
// == mock vscode.workspace before importing the module under test == //
const mockTextDocuments = [];
const mockVscode = {
    workspace: {
        get textDocuments() { return mockTextDocuments; },
    },
};
// Override require for vscode module
const module_1 = __importDefault(require("module"));
const originalResolveFilename = module_1.default._resolveFilename;
module_1.default._resolveFilename = function (request, ...args) {
    if (request === "vscode") {
        return request;
    }
    return originalResolveFilename.call(this, request, ...args);
};
const originalLoad = module_1.default.prototype.load;
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
// Now import the module under test
const fileContextGatherer_1 = require("../fileContextGatherer");
// == helper to create a mock TextDocument == //
function createDoc(fsPath, content, scheme = "file") {
    return {
        uri: { scheme, fsPath, toString: () => `${scheme}://${fsPath}` },
        getText: () => content,
    };
}
describe("gatherFileContexts", () => {
    beforeEach(() => {
        mockTextDocuments.length = 0;
    });
    // == returns empty when no documents open == //
    it("returns empty array when no documents are open", () => {
        const result = (0, fileContextGatherer_1.gatherFileContexts)();
        assert.strictEqual(result.length, 0);
    });
    // == filters to file:// scheme only == //
    it("gathers only file:// scheme documents", () => {
        mockTextDocuments.push(createDoc("/src/app.ts", "const a = 1;", "file"), createDoc("Untitled-1", "draft", "untitled"), createDoc("/repo/file.ts", "diff content", "git"), createDoc("/src/utils.ts", "export const b = 2;", "file"));
        const result = (0, fileContextGatherer_1.gatherFileContexts)();
        assert.strictEqual(result.length, 2);
        assert.strictEqual(result[0].filePath, "/src/app.ts");
        assert.strictEqual(result[1].filePath, "/src/utils.ts");
    });
    // == truncates content over 10,000 chars == //
    it("truncates content exceeding 10,000 characters", () => {
        const longContent = "x".repeat(15_000);
        mockTextDocuments.push(createDoc("/big.ts", longContent));
        const result = (0, fileContextGatherer_1.gatherFileContexts)();
        assert.strictEqual(result.length, 1);
        assert.ok(result[0].content.length < 15_000);
        assert.ok(result[0].content.length > 10_000); // 10000 + truncation marker
        assert.ok(result[0].content.endsWith("// ... (truncated)"));
    });
    // == preserves filePath from uri.fsPath == //
    it("preserves filePath from document uri", () => {
        mockTextDocuments.push(createDoc("/home/user/project/main.ts", "code here"));
        const result = (0, fileContextGatherer_1.gatherFileContexts)();
        assert.strictEqual(result[0].filePath, "/home/user/project/main.ts");
    });
    // == handles multiple documents == //
    it("returns all open file documents", () => {
        mockTextDocuments.push(createDoc("/a.ts", "file a"), createDoc("/b.ts", "file b"), createDoc("/c.ts", "file c"));
        const result = (0, fileContextGatherer_1.gatherFileContexts)();
        assert.strictEqual(result.length, 3);
        assert.strictEqual(result[0].content, "file a");
        assert.strictEqual(result[1].content, "file b");
        assert.strictEqual(result[2].content, "file c");
    });
});
//# sourceMappingURL=fileContextGatherer.test.js.map