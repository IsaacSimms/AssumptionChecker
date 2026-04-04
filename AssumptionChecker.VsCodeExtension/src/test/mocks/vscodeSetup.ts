// <summary>
// Registers a shared vscode mock in require.cache ONCE.
// Must be imported before any module that imports "vscode".
// Exports the mock for test manipulation (e.g. setting textDocuments).
// </summary>

import Module from "module";
import { MockStatusBarItem, ThemeColor } from "./vscode";

// == shared mock state == //
const sharedMock = {
    workspace: {
        textDocuments: [] as any[],
        getConfiguration: (section?: string) => ({
            get: <T>(key: string, defaultValue?: T): T | undefined => defaultValue,
        }),
    },
    window: {
        createStatusBarItem: (alignment?: number, priority?: number) => {
            const item = new MockStatusBarItem();
            item.alignment = alignment ?? 1;
            item.priority = priority ?? 0;
            sharedMock._lastStatusBarItem = item;
            return item;
        },
        showErrorMessage: async (msg: string) => undefined,
        showInformationMessage: async (msg: string) => undefined,
        registerWebviewViewProvider: (viewId: string, provider: any) => ({
            dispose: () => {},
        }),
    },
    StatusBarAlignment: { Left: 1, Right: 2 },
    ThemeColor: ThemeColor,
    Uri: {
        file: (path: string) => ({ scheme: "file", fsPath: path, toString: () => `file://${path}` }),
        parse: (value: string) => ({
            scheme: value.split("://")[0] || "file",
            fsPath: value.replace(/^[a-z]+:\/\//, ""),
            toString: () => value,
        }),
    },
    // internal: last created status bar item for assertions
    _lastStatusBarItem: null as MockStatusBarItem | null,
};

// == register in require.cache — only once == //
const resolveOrig = (Module as any)._resolveFilename;
(Module as any)._resolveFilename = function (request: string, ...args: any[]) {
    if (request === "vscode") { return request; }
    return resolveOrig.call(this, request, ...args);
};

require.cache["vscode"] = {
    id: "vscode",
    filename: "vscode",
    loaded: true,
    exports: sharedMock,
    parent: null,
    children: [],
    paths: [],
    path: "",
    require: require,
    isPreloading: false,
} as any;

export { sharedMock };
