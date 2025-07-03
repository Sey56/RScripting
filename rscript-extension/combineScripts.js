"use strict";
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
Object.defineProperty(exports, "__esModule", { value: true });
exports.combineScripts = combineScripts;
const fs = __importStar(require("fs"));
const path = __importStar(require("path"));
async function combineScripts(rootPath, entryFileName = "Main.cs") {
    const scriptsDir = path.join(rootPath, "Scripts");
    const entryFilePath = path.join(scriptsDir, entryFileName);
    const tempDir = path.join(rootPath, "Temp");
    const outputFile = path.join(tempDir, "CombinedScript.cs");
    if (!fs.existsSync(entryFilePath)) {
        throw new Error(`Entry script '${entryFileName}' not found in Scripts folder.`);
    }
    if (!fs.existsSync(tempDir)) {
        fs.mkdirSync(tempDir);
    }
    const entryContent = fs.readFileSync(entryFilePath, "utf-8");
    const usingRegex = /^using\s+[\w\.\s]+;/gm;
    const classRegex = /\b(class|record|interface|enum)\s+\w+/g;
    const allScriptFiles = fs
        .readdirSync(scriptsDir)
        .filter(f => f.endsWith(".cs") && f !== entryFileName);
    // Detect referenced types based on new TypeName() or TypeName. usage
    const referencedTypeNames = new Set();
    const matchTypeRefs = entryContent.match(/\bnew\s+(\w+)\b|\b(\w+)\s*\./g) || [];
    for (const ref of matchTypeRefs) {
        const match = ref.match(/\w+/);
        if (match) {
            referencedTypeNames.add(match[0]);
        }
    }
    // Gather script pieces
    const usingSet = new Set();
    const entryLines = entryContent.split("\n");
    const entryTopLevel = [];
    const entryTypes = [];
    let insideTypeBlock = false;
    for (const line of entryLines) {
        if (!insideTypeBlock && line.match(usingRegex)) {
            usingSet.add(line.trim());
        }
        else if (!insideTypeBlock && line.match(/^\s*(class|record|interface|enum)\b/)) {
            insideTypeBlock = true;
            entryTypes.push(line);
        }
        else if (insideTypeBlock) {
            entryTypes.push(line);
        }
        else {
            entryTopLevel.push(line);
        }
    }
    const referencedTypes = [];
    for (const file of allScriptFiles) {
        const fullPath = path.join(scriptsDir, file);
        const content = fs.readFileSync(fullPath, "utf-8");
        const nameWithoutExt = path.basename(file, ".cs");
        if (referencedTypeNames.has(nameWithoutExt)) {
            const usings = content.match(usingRegex) || [];
            usings.forEach(u => usingSet.add(u.trim()));
            const body = content
                .split("\n")
                .filter(line => !line.match(usingRegex))
                .join("\n")
                .trim();
            referencedTypes.push(body);
        }
    }
    const fullScript = [
        "// ------------------------------",
        "// 🔧 Auto-generated CombinedScript.cs",
        "// ------------------------------\n",
        [...usingSet].sort().join("\n"),
        "\n\n// Top-level code\n",
        entryTopLevel.join("\n"),
        "\n\n// Referenced type definitions\n",
        referencedTypes.join("\n\n"),
        "\n\n// Types from Main.cs\n",
        entryTypes.join("\n"),
    ].join("\n");
    fs.writeFileSync(outputFile, fullScript, "utf-8");
    return outputFile;
}
//# sourceMappingURL=combineScripts.js.map