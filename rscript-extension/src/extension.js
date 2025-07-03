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
exports.activate = activate;
exports.deactivate = deactivate;
const combineScripts_1 = require("./combineScripts");
const vscode = __importStar(require("vscode"));
const fs = __importStar(require("fs"));
const path = __importStar(require("path"));
const child_process_1 = require("child_process");
const util_1 = require("util");
const execPromise = (0, util_1.promisify)(child_process_1.exec);
function activate(context) {
    console.log("RScript extension is now active!");
    // Command to initialize the workspace
    let initializeWorkspace = vscode.commands.registerCommand("rscript.initializeWorkspace", async () => {
        const workspaceFolders = vscode.workspace.workspaceFolders;
        if (!workspaceFolders) {
            vscode.window.showErrorMessage("Please open a workspace folder before initializing RScript.");
            return;
        }
        const rootPath = workspaceFolders[0].uri.fsPath;
        const stubsPath = path.join(rootPath, "Stubs");
        const scriptsPath = path.join(rootPath, "Scripts");
        const toolsPath = path.join(rootPath, "Tools");
        try {
            // Create Stubs, Scripts, and Tools folders
            if (!fs.existsSync(stubsPath)) {
                fs.mkdirSync(stubsPath);
            }
            if (!fs.existsSync(scriptsPath)) {
                fs.mkdirSync(scriptsPath);
            }
            if (!fs.existsSync(toolsPath)) {
                fs.mkdirSync(toolsPath);
            }
            // Copy rscript-bridge.exe, .dll, and .runtimeconfig.json to the Tools folder
            const extensionPath = context.extensionPath;
            const bridgeExeSource = path.join(extensionPath, "bin", "rscript-bridge.exe");
            const bridgeDllSource = path.join(extensionPath, "bin", "rscript-bridge.dll");
            const bridgeConfigSource = path.join(extensionPath, "bin", "rscript-bridge.runtimeconfig.json");
            const bridgeExeDest = path.join(toolsPath, "rscript-bridge.exe");
            const bridgeDllDest = path.join(toolsPath, "rscript-bridge.dll");
            const bridgeConfigDest = path.join(toolsPath, "rscript-bridge.runtimeconfig.json");
            if (!fs.existsSync(bridgeExeSource) ||
                !fs.existsSync(bridgeDllSource) ||
                !fs.existsSync(bridgeConfigSource)) {
                throw new Error("rscript-bridge.exe, .dll, or .runtimeconfig.json not found in the extension. Please ensure they are included in the bin folder.");
            }
            fs.copyFileSync(bridgeExeSource, bridgeExeDest);
            fs.copyFileSync(bridgeDllSource, bridgeDllDest);
            fs.copyFileSync(bridgeConfigSource, bridgeConfigDest);
            // Create global.json to pin the .NET SDK version to 8.0.411
            const globalJsonContent = `
{
  "sdk": {
    "version": "8.0.411"
  }
}
            `.trim();
            fs.writeFileSync(path.join(rootPath, "global.json"), globalJsonContent);
            // Get the Revit installation path from settings
            const config = vscode.workspace.getConfiguration("rscript");
            const revitInstallPath = config.get("revitInstallPath", "C:\\Program Files\\Autodesk\\Revit 2025");
            // Create RScript.csproj (include both Stubs and Scripts)
            const csprojContent = `
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
    <OutputType>Library</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="RevitAPI">
      <HintPath>${revitInstallPath}\\RevitAPI.dll</HintPath>
      <Private>False</Private>
    </Reference>
    <Reference Include="RevitAPIUI">

      <HintPath>${revitInstallPath}\\RevitAPIUI.dll</HintPath>
      <Private>False</Private>
    </Reference>
  </ItemGroup>
</Project>
            `.trim();
            fs.writeFileSync(path.join(rootPath, "RScript.csproj"), csprojContent);
            // Create RScriptAddinServices.cs (stub file)
            const stubContent = `
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;

namespace RScript.Addin.Services
{
    public class ScriptGlobals
    {
        public static UIApplication? UIApp { get; set; }
        public static UIDocument? UIDoc { get; set; }
        public static Document? Doc { get; set; }
        public static string? OutputPipeName { get; set; }

        public static void Print(string message)
        {
            // IntelliSense stub; at runtime this sends to the VS Code pipe
        }
    }

    public static class Tx
    {
        public static void TransactWithDoc(Document doc, string transactionName, Action<Document> action)
        {
            if (doc == null)
                throw new InvalidOperationException("Document is null in Transact");

            using var transaction = new Transaction(doc, transactionName);
            try
            {
                _ = transaction.Start();
                action(doc);
                _ = transaction.Commit();
            }
            catch (Exception)
            {
                if (transaction.GetStatus() == TransactionStatus.Started)
                {
                    _ = transaction.RollBack();
                }
                throw;
            }
        }
    }

    public static class Helpers
    {
        public static void Transact(string name, Action<Document> action)
        {
            // IntelliSense stub; runtime version injected by CodeRunner.cs
        }
    }
}
`.trim();
            fs.writeFileSync(path.join(stubsPath, "RScriptAddinServices.cs"), stubContent);
            const globalUsingsContent = `
global using static RScript.Addin.Services.ScriptGlobals;
global using static RScript.Addin.Services.Helpers;
global using static RScript.Addin.Services.Tx;
`.trim();
            fs.writeFileSync(path.join(stubsPath, "GlobalUsings.cs"), globalUsingsContent);
            // Create a sample script
            // Default SpiralCreator class
            const spiralCreatorContent = `
using Autodesk.Revit.DB;

public class SpiralCreator
{
    public void CreateSpiral(Document doc, string levelName, double maxRadiusCm, int numTurns, double angleResolutionDegrees)
    {
        Level? level = new FilteredElementCollector(doc)
            .OfClass(typeof(Level))
            .Cast<Level>()
            .FirstOrDefault(l => l.Name == levelName) ?? throw new Exception($"\\"{levelName}\\" not found.");

        double maxRadiusFt = UnitUtils.ConvertToInternalUnits(maxRadiusCm, UnitTypeId.Centimeters);
        double angleResRad = angleResolutionDegrees * Math.PI / 180;

        var curves = new List<Curve>();
        XYZ origin = XYZ.Zero;

        for (int i = 0; i < numTurns * 360 / angleResolutionDegrees; i++)
        {
            double angle1 = i * angleResRad;
            double angle2 = (i + 1) * angleResRad;

            double radius1 = maxRadiusFt * angle1 / (numTurns * 2 * Math.PI);
            double radius2 = maxRadiusFt * angle2 / (numTurns * 2 * Math.PI);

            XYZ pt1 = new(radius1 * Math.Cos(angle1), radius1 * Math.Sin(angle1), level.Elevation);
            XYZ pt2 = new(radius2 * Math.Cos(angle2), radius2 * Math.Sin(angle2), level.Elevation);

            Line line = Line.CreateBound(pt1, pt2);
            if (line.Length > 0.0026)
            {
                curves.Add(line);
            }
        }

        Transact("Create Spiral", doc =>
        {
            var sketch = SketchPlane.Create(doc, Plane.CreateByNormalAndOrigin(XYZ.BasisZ, origin));
            foreach (var curve in curves)
            {
                doc.Create.NewModelCurve(curve, sketch);
            }
        });
    }
}
`.trim();
            fs.writeFileSync(path.join(scriptsPath, "SpiralCreator.cs"), spiralCreatorContent);
            // Main entrypoint script
            const mainScriptContent = `
var spiral = new SpiralCreator();
spiral.CreateSpiral(Doc, "Level 1", 100, 5, 10);

// You can define more types below this line — they'll be included automatically
`.trim();
            fs.writeFileSync(path.join(scriptsPath, "Main.cs"), mainScriptContent);
            // Create .vscode folder and tasks.json
            const vscodePath = path.join(rootPath, ".vscode");
            if (!fs.existsSync(vscodePath)) {
                fs.mkdirSync(vscodePath);
            }
            const tasksJsonContent = `
{
    "version": "2.0.0",
    "tasks": [
        {
            "label": "Send to Revit",
            "type": "shell",
            "command": "\${workspaceFolder}/Tools/rscript-bridge.exe \${file}",
            "problemMatcher": [],
            "group": {
                "kind": "build",
                "isDefault": true
            }
        }
    ]
}
            `.trim();
            fs.writeFileSync(path.join(vscodePath, "tasks.json"), tasksJsonContent);
            // Prompt to restore the project
            const restoreAction = "Restore";
            const action = await vscode.window.showInformationMessage("RScript workspace initialized! Please restore the project to enable IntelliSense.", restoreAction);
            if (action === restoreAction) {
                try {
                    const restoreCommand = "dotnet restore";
                    const { stdout, stderr } = await execPromise(restoreCommand, {
                        cwd: rootPath,
                    });
                    if (stdout) {
                        console.log(`[Restore] ${stdout}`);
                    }
                    if (stderr) {
                        console.warn(`[Restore Warning] ${stderr}`);
                    }
                    vscode.window.showInformationMessage("Project restored successfully. IntelliSense should now be enabled.");
                }
                catch (restoreError) {
                    vscode.window.showWarningMessage(`Failed to restore project: ${restoreError.message}. You can still write and run scripts, but IntelliSense may not work. Try running 'dotnet restore' manually.`);
                }
            }
            // Open the sample script
            const sampleScriptUri = vscode.Uri.file(path.join(scriptsPath, "Main.cs"));
            const doc = await vscode.workspace.openTextDocument(sampleScriptUri);
            await vscode.window.showTextDocument(doc);
        }
        catch (error) {
            vscode.window.showErrorMessage(`Failed to initialize RScript workspace: ${error.message}`);
        }
    });
    // Command to run the script
    let runScript = vscode.commands.registerCommand("rscript.runScript", async () => {
        const workspaceFolders = vscode.workspace.workspaceFolders;
        if (!workspaceFolders) {
            vscode.window.showErrorMessage("Please open a workspace folder to run the script.");
            return;
        }
        const rootPath = workspaceFolders[0].uri.fsPath;
        const bridgeExePath = path.join(rootPath, "Tools", "rscript-bridge.exe");
        if (!fs.existsSync(bridgeExePath)) {
            vscode.window.showErrorMessage('rscript-bridge.exe not found. Please run "RScript: Initialize Workspace" to set up the workspace.');
            return;
        }
        try {
            // Combine scripts into one cohesive file
            const combinedScriptPath = await (0, combineScripts_1.combineScripts)(rootPath, "Main.cs");
            const { stdout, stderr } = await execPromise(`"${bridgeExePath}" "${combinedScriptPath}"`);
            if (stderr) {
                vscode.window.showErrorMessage(`Error sending script to Revit: ${stderr}`);
                return;
            }
            vscode.window.showInformationMessage(stdout.trim() || "Script sent to Revit successfully.");
        }
        catch (error) {
            vscode.window.showErrorMessage(`Failed to run RScript: ${error.message}`);
        }
    });
    context.subscriptions.push(initializeWorkspace);
    context.subscriptions.push(runScript);
}
function deactivate() { }
//# sourceMappingURL=extension.js.map