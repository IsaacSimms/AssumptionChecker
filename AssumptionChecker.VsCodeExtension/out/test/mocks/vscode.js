"use strict";
// <summary>
// Manual mock of the VS Code API for unit testing.
// Provides stub implementations of StatusBarItem, TextDocument,
// SecretStorage, workspace, window, Uri, ThemeColor, EventEmitter.
// </summary>
Object.defineProperty(exports, "__esModule", { value: true });
exports.Uri = exports.workspace = exports.window = exports.StatusBarAlignment = exports.ThemeColor = exports.MockSecretStorage = exports.MockStatusBarItem = void 0;
exports.createMockSecretStorage = createMockSecretStorage;
exports.createMockTextDocument = createMockTextDocument;
exports.createMockWebviewView = createMockWebviewView;
// == MockStatusBarItem == //
class MockStatusBarItem {
    text = "";
    tooltip = undefined;
    backgroundColor = undefined;
    alignment = 1;
    priority = 0;
    shown = false;
    show() { this.shown = true; }
    hide() { this.shown = false; }
    dispose() { this.shown = false; }
}
exports.MockStatusBarItem = MockStatusBarItem;
// == MockSecretStorage — in-memory Map-based == //
class MockSecretStorage {
    store = new Map();
    async get(key) {
        return this.store.get(key);
    }
    async store_(key, value) {
        this.store.set(key, value);
    }
    async delete(key) {
        this.store.delete(key);
    }
    // alias to match vscode.SecretStorage.store
    async storeValue(key, value) {
        this.store.set(key, value);
    }
}
exports.MockSecretStorage = MockSecretStorage;
// == createMockSecretStorage — returns object matching vscode.SecretStorage == //
function createMockSecretStorage() {
    const map = new Map();
    return {
        get: async (key) => map.get(key),
        store: async (key, value) => { map.set(key, value); },
        delete: async (key) => { map.delete(key); },
        onDidChange: () => ({ dispose: () => { } }),
        _map: map, // exposed for test assertions
    };
}
// == MockTextDocument == //
function createMockTextDocument(fsPath, content, scheme = "file") {
    return {
        uri: {
            scheme,
            fsPath,
            toString: () => `${scheme}://${fsPath}`,
        },
        getText: () => content,
        fileName: fsPath,
        languageId: "plaintext",
        version: 1,
        isDirty: false,
        isUntitled: scheme === "untitled",
        lineCount: content.split("\n").length,
    };
}
// == MockWebviewView — for SidebarProvider testing == //
function createMockWebviewView() {
    const messages = [];
    let messageHandler;
    const webviewView = {
        webview: {
            options: {},
            html: "",
            postMessage: (msg) => {
                messages.push(msg);
                return Promise.resolve(true);
            },
            onDidReceiveMessage: (handler) => {
                messageHandler = handler;
                return { dispose: () => { } };
            },
            asWebviewUri: (uri) => uri,
            cspSource: "https://test.vscode.com",
        },
        visible: true,
        onDidDispose: () => ({ dispose: () => { } }),
        onDidChangeVisibility: () => ({ dispose: () => { } }),
    };
    const fireMessage = (msg) => {
        if (messageHandler) {
            messageHandler(msg);
        }
    };
    return { webviewView, messages, fireMessage };
}
// == ThemeColor stub == //
class ThemeColor {
    id;
    constructor(id) { this.id = id; }
}
exports.ThemeColor = ThemeColor;
// == StatusBarAlignment enum == //
exports.StatusBarAlignment = {
    Left: 1,
    Right: 2,
};
// == mock vscode.window == //
exports.window = {
    createStatusBarItem: (alignment, priority) => {
        const item = new MockStatusBarItem();
        item.alignment = alignment ?? 1;
        item.priority = priority ?? 0;
        return item;
    },
    showErrorMessage: async (msg) => undefined,
    showInformationMessage: async (msg) => undefined,
    registerWebviewViewProvider: (viewId, provider) => ({
        dispose: () => { },
    }),
};
// == mock vscode.workspace == //
exports.workspace = {
    textDocuments: [],
    getConfiguration: (section) => ({
        get: (key, defaultValue) => defaultValue,
    }),
};
// == mock vscode.Uri == //
exports.Uri = {
    file: (path) => ({
        scheme: "file",
        fsPath: path,
        toString: () => `file://${path}`,
    }),
    parse: (value) => ({
        scheme: value.split("://")[0] || "file",
        fsPath: value.replace(/^[a-z]+:\/\//, ""),
        toString: () => value,
    }),
};
//# sourceMappingURL=vscode.js.map