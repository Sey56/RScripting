# RScript Extension

Run C# scripts inside Autodesk Revit directly from VS Code.

## ✨ Features

- Initialize a Revit scripting workspace with full IntelliSense and build config
- Run scripts in live Revit sessions via named pipe (`RScriptPipe`)
- Auto-generate `.csproj`, API stubs, and sample scripts
- Unified bridge via `rscript-bridge.exe` — no more SendToRevit.exe

## 🛠 Requirements

- Revit 2025 installed (for RevitAPI.dll and RevitAPIUI.dll)
- RScript Add-in loaded (listening on `RScriptPipe`)
- .NET 8 SDK (for building)
- Node.js (for extension packaging)
- Windows only (Revit is required)

## 🚀 Getting Started

1. Clone [RScripting](https://github.com/Sey56/RScripting)
2. Run `./build.sh` from Git Bash to package and install the extension
3. Open `RScriptWorkspace/` in VS Code
4. Run `RScript: Initialize Workspace` from the Command Palette
5. Edit `Scripts/SampleScript.cs`
6. Press `Ctrl + Alt + R` to run the script — results stream from Revit

## 🔧 Configuration

Set your Revit path if it differs from the default:

```json
"rscript.revitInstallPath": "C:\\Program Files\\Autodesk\\Revit 2025"



🧩 Contributing
Issues and pull requests welcome at:
https://github.com/Sey56/RScripting
© 2025 Seyoum Hagos — MIT License