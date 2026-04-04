// <summary>
// Manual mock of the VS Code API for unit testing.
// Provides stub implementations of StatusBarItem, TextDocument,
// SecretStorage, workspace, window, Uri, ThemeColor, EventEmitter.
// </summary>

// == MockStatusBarItem == //
export class MockStatusBarItem {
    text = "";
    tooltip: string | undefined = undefined;
    backgroundColor: any = undefined;
    alignment = 1;
    priority = 0;
    shown = false;

    show(): void { this.shown = true; }
    hide(): void { this.shown = false; }
    dispose(): void { this.shown = false; }
}

// == MockSecretStorage — in-memory Map-based == //
export class MockSecretStorage {
    private store = new Map<string, string>();

    async get(key: string): Promise<string | undefined> {
        return this.store.get(key);
    }

    async store_(key: string, value: string): Promise<void> {
        this.store.set(key, value);
    }

    async delete(key: string): Promise<void> {
        this.store.delete(key);
    }

    // alias to match vscode.SecretStorage.store
    async storeValue(key: string, value: string): Promise<void> {
        this.store.set(key, value);
    }
}

// == createMockSecretStorage — returns object matching vscode.SecretStorage == //
export function createMockSecretStorage(): any {
    const map = new Map<string, string>();
    return {
        get: async (key: string) => map.get(key),
        store: async (key: string, value: string) => { map.set(key, value); },
        delete: async (key: string) => { map.delete(key); },
        onDidChange: () => ({ dispose: () => {} }),
        _map: map, // exposed for test assertions
    };
}

// == MockTextDocument == //
export function createMockTextDocument(
    fsPath: string,
    content: string,
    scheme = "file",
): any {
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
export function createMockWebviewView(): {
    webviewView: any;
    messages: any[];
    fireMessage: (msg: any) => void;
} {
    const messages: any[] = [];
    let messageHandler: ((msg: any) => void) | undefined;

    const webviewView = {
        webview: {
            options: {} as any,
            html: "",
            postMessage: (msg: any) => {
                messages.push(msg);
                return Promise.resolve(true);
            },
            onDidReceiveMessage: (handler: (msg: any) => void) => {
                messageHandler = handler;
                return { dispose: () => {} };
            },
            asWebviewUri: (uri: any) => uri,
            cspSource: "https://test.vscode.com",
        },
        visible: true,
        onDidDispose: () => ({ dispose: () => {} }),
        onDidChangeVisibility: () => ({ dispose: () => {} }),
    };

    const fireMessage = (msg: any) => {
        if (messageHandler) { messageHandler(msg); }
    };

    return { webviewView, messages, fireMessage };
}

// == ThemeColor stub == //
export class ThemeColor {
    id: string;
    constructor(id: string) { this.id = id; }
}

// == StatusBarAlignment enum == //
export const StatusBarAlignment = {
    Left: 1,
    Right: 2,
};

// == mock vscode.window == //
export const window = {
    createStatusBarItem: (alignment?: number, priority?: number) => {
        const item = new MockStatusBarItem();
        item.alignment = alignment ?? 1;
        item.priority = priority ?? 0;
        return item;
    },
    showErrorMessage: async (msg: string) => undefined,
    showInformationMessage: async (msg: string) => undefined,
    registerWebviewViewProvider: (viewId: string, provider: any) => ({
        dispose: () => {},
    }),
};

// == mock vscode.workspace == //
export const workspace = {
    textDocuments: [] as any[],
    getConfiguration: (section?: string) => ({
        get: <T>(key: string, defaultValue?: T): T | undefined => defaultValue,
    }),
};

// == mock vscode.Uri == //
export const Uri = {
    file: (path: string) => ({
        scheme: "file",
        fsPath: path,
        toString: () => `file://${path}`,
    }),
    parse: (value: string) => ({
        scheme: value.split("://")[0] || "file",
        fsPath: value.replace(/^[a-z]+:\/\//, ""),
        toString: () => value,
    }),
};
