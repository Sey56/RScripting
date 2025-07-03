import * as vscode from "vscode";
import * as fs from "fs";
import * as path from "path";
import { exec, spawn } from "child_process";
import { promisify } from "util";

const execPromise = promisify(exec);

export function activate(context: vscode.ExtensionContext) {
  console.log("RScript extension is now active!");
  const outputChannel = vscode.window.createOutputChannel("RScripting");
  context.subscriptions.push(outputChannel);

  // Command to initialize the workspace
  let initializeWorkspace = vscode.commands.registerCommand(
    "rscript.initializeWorkspace",
    async () => {
      const workspaceFolders = vscode.workspace.workspaceFolders;
      if (!workspaceFolders) {
        vscode.window.showErrorMessage(
          "Please open a workspace folder before initializing RScript."
        );
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

        // Define extension paths
        const extensionPath = context.extensionPath;
        const binPath = path.join(extensionPath, "bin");

        // === Copy rscript-bridge files ===
        const bridgeFiles = [
          "rscript-bridge.exe",
          "rscript-bridge.dll",
          "rscript-bridge.runtimeconfig.json",
        ];

        // === Copy RScript.Combiner files ===
        const combinerFiles = [
          "RScript.Combiner.exe",
          "RScript.Combiner.dll",
          "RScript.Combiner.runtimeconfig.json",
        ];

        const allToolFiles = [...bridgeFiles, ...combinerFiles];

        for (const filename of allToolFiles) {
          const src = path.join(binPath, filename);
          const dest = path.join(toolsPath, filename);
          if (!fs.existsSync(src)) {
            throw new Error(`❌ Required file missing from bin: ${filename}`);
          }
          fs.copyFileSync(src, dest);
        }
        // === Copy Roslyn/Immutable dependencies required by RScript.Combiner ===
        const roslynDependencies = [
          "Microsoft.CodeAnalysis.dll",
          "Microsoft.CodeAnalysis.CSharp.dll",
          "System.Collections.Immutable.dll",
        ];

        for (const filename of roslynDependencies) {
          const src = path.join(binPath, filename);
          const dest = path.join(toolsPath, filename);
          if (!fs.existsSync(src)) {
            throw new Error(
              `❌ Required dependency missing in bin/: ${filename}`
            );
          }
          fs.copyFileSync(src, dest);
        }
        // Create global.json to pin the .NET SDK version
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
        const revitInstallPath = config.get<string>(
          "revitInstallPath",
          "C:\\Program Files\\Autodesk\\Revit 2025"
        );

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
        fs.writeFileSync(
          path.join(stubsPath, "RScriptAddinServices.cs"),
          stubContent
        );

        const globalUsingsContent = `
global using static RScript.Addin.Services.ScriptGlobals;
global using static RScript.Addin.Services.Helpers;
global using static RScript.Addin.Services.Tx;
`.trim();

        fs.writeFileSync(
          path.join(stubsPath, "GlobalUsings.cs"),
          globalUsingsContent
        );

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

        fs.writeFileSync(
          path.join(scriptsPath, "SpiralCreator.cs"),
          spiralCreatorContent
        );

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
        const action = await vscode.window.showInformationMessage(
          "RScript workspace initialized! Please restore the project to enable IntelliSense.",
          restoreAction
        );
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

            vscode.window.showInformationMessage(
              "Project restored successfully. IntelliSense should now be enabled."
            );
          } catch (restoreError: any) {
            vscode.window.showWarningMessage(
              `Failed to restore project: ${restoreError.message}. You can still write and run scripts, but IntelliSense may not work. Try running 'dotnet restore' manually.`
            );
          }
        }

        // Open the sample script
        const sampleScriptUri = vscode.Uri.file(
          path.join(scriptsPath, "Main.cs")
        );
        const doc = await vscode.workspace.openTextDocument(sampleScriptUri);
        await vscode.window.showTextDocument(doc);
      } catch (error: any) {
        vscode.window.showErrorMessage(
          `Failed to initialize RScript workspace: ${error.message}`
        );
      }
    }
  );

  let runScript = vscode.commands.registerCommand(
    "rscript.runScript",
    async () => {
      const workspaceFolders = vscode.workspace.workspaceFolders;
      if (!workspaceFolders) {
        vscode.window.showErrorMessage(
          "Please open a workspace folder to run the script."
        );
        return;
      }

      const rootPath = workspaceFolders[0].uri.fsPath;
      const bridgeExePath = path.join(rootPath, "Tools", "rscript-bridge.exe");

      if (!fs.existsSync(bridgeExePath)) {
        vscode.window.showErrorMessage(
          'rscript-bridge.exe not found. Please run "RScript: Initialize Workspace" to set up the workspace.'
        );
        return;
      }

      const combinerPath = path.join(rootPath, "Tools", "RScript.Combiner.exe");

      const tmp = require("tmp");

      // const logsDir = path.join(rootPath, "Logs");
      // fs.mkdirSync(logsDir, { recursive: true });

      const tempFile = tmp.fileSync({
        prefix: "RScript_",
        postfix: ".cs",
        discardDescriptor: true,
      });
      const combinedScriptPath = tempFile.name;
      console.log("🧪 Combined script lives at:", combinedScriptPath);

      async function runCombiner(
        combinerPath: string,
        rootPath: string,
        outputPath: string
      ): Promise<void> {
        return new Promise((resolve, reject) => {
          console.log("📦 combinerPath =", combinerPath);
          console.log("📦 working dir  =", path.dirname(combinerPath));
          console.log("📄 outputPath   =", outputPath);
          console.log("📁 exists .exe  =", fs.existsSync(combinerPath));
          console.log(
            "📁 exists dir   =",
            fs.existsSync(path.dirname(combinerPath))
          );
          console.log(
            "📁 exists DLLs  =",
            [
              "Microsoft.CodeAnalysis.dll",
              "Microsoft.CodeAnalysis.CSharp.dll",
              "System.Collections.Immutable.dll",
            ].map((dll) =>
              fs.existsSync(path.join(path.dirname(combinerPath), dll))
            )
          );
          const proc = spawn(combinerPath, [rootPath, "Main.cs", outputPath], {
            cwd: path.dirname(combinerPath),
            shell: false,
          });

          proc.stdout.on("data", (data) => {
            console.log(`🟢 Combiner: ${data}`);
          });

          proc.stderr.on("data", (data) => {
            console.error(`🔴 Combiner Error: ${data}`);
          });

          proc.on("close", (code) => {
            if (code === 0) {
              resolve();
            } else {
              reject(new Error(`Combiner exited with code ${code}`));
            }
          });
        });
      }

      try {
        await runCombiner(combinerPath, rootPath, combinedScriptPath);

        // Optional: Archive to Logs/
        // const timestamp = new Date().toISOString().replace(/[:.]/g, "-");
        // const logCopyPath = path.join(
        //   logsDir,
        //   `CombinedScript_${timestamp}.cs`
        // );
        // fs.copyFileSync(combinedScriptPath, logCopyPath);

        const { stdout, stderr } = await execPromise(
          `${bridgeExePath} "${combinedScriptPath}"`
        );

        outputChannel.appendLine("=== RScript Execution ===");
        outputChannel.appendLine(
          `➤ CombinedScript Path: ${combinedScriptPath}`
        );
        outputChannel.appendLine(stdout.trim() || "[No output]");
        outputChannel.appendLine("=========================\n");
        outputChannel.show(true); // Optional: auto-show Output tab

        if (stderr) {
          outputChannel.appendLine("❌ Error from bridge:");
          outputChannel.appendLine(stderr.trim());
          outputChannel.show(true);
          vscode.window.showErrorMessage(
            `Error sending script to Revit: ${stderr}`
          );
          return;
        }

        vscode.window.showInformationMessage(
          stdout.trim() || "Script sent to Revit successfully."
        );
      } catch (error: any) {
        outputChannel.appendLine(`❌ Script error: ${error.message}`);
        outputChannel.show(true);
        vscode.window.showErrorMessage(
          `Failed to run RScript: ${error.message}`
        );
      }
    }
  );

  context.subscriptions.push(initializeWorkspace);
  context.subscriptions.push(runScript);
}

export function deactivate() {}
