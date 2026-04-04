"use strict";
// <summary>
// Registers a shared vscode mock in require.cache ONCE.
// Must be imported before any module that imports "vscode".
// Exports the mock for test manipulation (e.g. setting textDocuments).
// </summary>
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.sharedMock = void 0;
const module_1 = __importDefault(require("module"));
const vscode_1 = require("./vscode");
// == shared mock state == //
const sharedMock = {
    workspace: {
        textDocuments: [],
        getConfiguration: (section) => ({
            get: (key, defaultValue) => defaultValue,
        }),
    },
    window: {
        createStatusBarItem: (alignment, priority) => {
            const item = new vscode_1.MockStatusBarItem();
            item.alignment = alignment ?? 1;
            item.priority = priority ?? 0;
            sharedMock._lastStatusBarItem = item;
            return item;
        },
        showErrorMessage: async (msg) => undefined,
        showInformationMessage: async (msg) => undefined,
        registerWebviewViewProvider: (viewId, provider) => ({
            dispose: () => { },
        }),
    },
    StatusBarAlignment: { Left: 1, Right: 2 },
    ThemeColor: vscode_1.ThemeColor,
    Uri: {
        file: (path) => ({ scheme: "file", fsPath: path, toString: () => `file://${path}` }),
        parse: (value) => ({
            scheme: value.split("://")[0] || "file",
            fsPath: value.replace(/^[a-z]+:\/\//, ""),
            toString: () => value,
        }),
    },
    // internal: last created status bar item for assertions
    _lastStatusBarItem: null,
};
exports.sharedMock = sharedMock;
// == register in require.cache — only once == //
const resolveOrig = module_1.default._resolveFilename;
module_1.default._resolveFilename = function (request, ...args) {
    if (request === "vscode") {
        return request;
    }
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
};
//# sourceMappingURL=vscodeSetup.js.map