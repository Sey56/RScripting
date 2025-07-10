📘 RScript Extension for VS Code
Send C# scripts directly to Autodesk Revit — no compile, no fuss.
This extension enables rapid BIM automation by connecting your RScripting workspace in VS Code to a Revit Add-in bridge.

✨ Features
- 🛠 One-click workspace scaffolding
- 🚀 Direct script execution in Revit from VS Code
- 🎯 Built-in IntelliSense stubs for Revit API
- 🔄 Automatic file wiring: Stubs/, Scripts/, Tools/
- ⚙️ Runs scripts using rscript-bridge.exe behind the scenes

🧪 Getting Started
- Install the RScript Add-in in Revit 2025
(Run build.sh or manually copy the DLL and .addin file)
- Install this extension in VS Code
- Open an empty workspace folder and run:
- RScript: Initialize Workspace from the Command Palette
- Modify or create scripts in Scripts/
- Run the script:
- RScript: Send Script to Revit
🗂 Workspace StructureYourWorkspace/
├── Scripts/
│   └── SampleScript.cs
├── Stubs/
│   └── RScriptAddinServices.cs
├── Tools/
│   └── rscript-bridge.exe
├── RScript.csproj
├── global.json
└── .vscode/
    └── tasks.json
💻 Requirements- .NET 8 SDK
- Revit 2025 installed
- RScripting Add-in deployed
