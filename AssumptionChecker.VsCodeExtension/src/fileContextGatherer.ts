// <summary>
// Gathers open editor file contents as FileContext[] for the analyze request.
// Filters to file:// scheme only, truncates to 10,000 chars per file.
// Mirrors the pattern in AssumptionCheckerViewModel.cs:216-245.
// </summary>

import * as vscode from "vscode";
import { FileContext } from "./types";

const MAX_FILE_LENGTH = 10_000;

// == gatherFileContexts == //
export function gatherFileContexts(): FileContext[] {
    const contexts: FileContext[] = [];

    for (const doc of vscode.workspace.textDocuments) {
        if (doc.uri.scheme !== "file") { continue; } // skip untitled, git, output, etc.

        let content = doc.getText();
        if (content.length > MAX_FILE_LENGTH) {
            content = content.substring(0, MAX_FILE_LENGTH) + "\n// ... (truncated)";
        }

        contexts.push({
            filePath: doc.uri.fsPath,
            content,
        });
    }

    return contexts;
}
