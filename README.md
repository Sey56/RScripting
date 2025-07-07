# 🏗️ RScripting

**Live C# scripting for Revit 2025 — powered by Roslyn, VS Code, and .NET 8.0**

RScripting is a developer-first framework that transforms how you automate Revit. It creates a live bridge between **Visual Studio Code** and **Revit 2025**, enabling you to write and execute native **C# scripts** with full IntelliSense, Revit API access, and modern .NET features — all without compiling or building an add-in.

---

## 🚀 Key Features

- 🔁 **Live Execution from VS Code**  
  Run C# scripts directly into Revit with a single keystroke — no build, no reload.

- 🧠 **Native .NET 8.0 Support**  
  Leverage the latest C# features and libraries without legacy baggage.

- 🪶 **Lightweight Architecture**  
  No persistent binaries, AppDomains, or installation steps for end users.

- 🌐 **Global Revit Context**  
  Access `Doc`, `UIDoc`, `Print()`, and `Transact()` in every script.

- 📢 **Human-Readable Output**  
  Errors and messages stream to both a toast overlay in Revit and the VS Code Output panel.

- 🧪 **Safe & Scoped Transactions**  
  Isolate changes with lightweight, script-scoped transaction helpers.

- 📦 **Portable Workspaces**  
  Scripts are just `.cs` files — easy to zip, version, and share.

---

## 🛠 Build Options

RScripting supports two build methods — both produce the same result:

### ✅ Recommended: Windows Command Line

```
./build.bat
```

- Works in **Command Prompt** or **PowerShell**
- No need for Git Bash or Unix tools
- Compiles all components, deploys the Revit add-in, and installs the VS Code extension

### 🐧 Alternative: Git Bash (if installed with Git)

```
./build.sh
```

- Works in **Git Bash** (included with Git for Windows)
- Useful for users familiar with Unix-style scripting

> 💡 Both scripts do the same thing — choose whichever fits your environment.

---

## 💻 System Requirements

| Component            | Version     |
|----------------------|-------------|
| Windows              | 10 or 11    |
| Revit                | 2025        |
| .NET SDK             | 8.0+        |
| Node.js + npm        | Latest LTS  |
| Visual Studio Code   | Latest      |
| Git (optional)       | For cloning and Git Bash |

---

## 🖲️ Revit Integration

When Revit starts, it loads the **RScript Add-In**, which currently includes a single toggle button:

### 🔘 RScript Server

- When toggled **ON**, it starts a lightweight server inside Revit.
- This server **listens for incoming script execution requests** from VS Code.
- Once connected, scripts can be executed live from your RScripting workspace.

> 💡 This toggle is the bridge between your development environment and the Revit model — enabling real-time automation without rebuilding or restarting.

---

## 🧪 Getting Started

### ➊ Open an Empty Folder in VS Code

Before initializing a workspace, open an **empty folder** in VS Code.  
The extension will scaffold the workspace in the currently open folder — it does not prompt you to select one.

---

### ➋ Initialize the Workspace

Run the following command:

```
Command Palette → RScript: Initialize Workspace
```

This will scaffold:

- `Scripts/`, `Stubs/`, and `Tools/` directories
- A `.csproj` preconfigured with Revit API references
- IntelliSense stubs (`Doc`, `UIDoc`, `Print`, etc.)
- A sample `Main.cs` script and transaction helpers
- A `global.json` file to pin the .NET SDK version

---

### ➌ Run Scripts

- Press `Ctrl + Alt + R` to execute the current script
- Or use `Command Palette → RScript: Run Script`

Output appears in:

- ✅ Revit’s toast overlay  
- ✅ VS Code Output panel under “RScripting”  
- ✅ Local log files (for advanced debugging or history)

All messages, exceptions, and diagnostics flow through `Print(...)` or error handlers — no guessing.

---

## 🧠 How It Works

### 🔹 RScript (Revit Add-In)
- Loaded via `RScript.Addin.addin` into Revit.
- Adds a toggle button: **RScript Server**.
- When toggled ON, it starts a listener that waits for scripts from VS Code.
- Executes the received C# script using Roslyn and streams results back.

### 🔹 rscript-extension (VS Code Extension)
- Provides commands like:
  - `RScript: Initialize Workspace`
  - `RScript: Run Script`
- Generates a `.csproj` with Revit API references (from local Revit install).
- Adds IntelliSense stubs and a `Scripts/` folder with `Main.cs`.

### 🔹 Main.cs (User Entry Point)
- Users write all top-level C# code here:
  - `using` statements
  - Top-level logic
  - Optional user-defined types
- Additional `.cs` files can be added to `Scripts/`, but must be referenced from `Main.cs`.

### 🔹 RScript.Combiner
- When a script is run:
  - Parses `Main.cs` for references to other scripts.
  - Combines all relevant files into a single `CombinedScript.cs`.
  - Stores it in the user's temp directory for debugging.

### 🔹 rscript-bridge
- Sends `CombinedScript.cs` to Revit via a named pipe or socket.
- Waits for execution results and streams them back to:
  - The VS Code Output panel
  - Revit’s toast overlay
  - Local log files

---

## 🧩 Repository Structure

```
RScripting/
├── .git/                     # Git metadata
├── .gitignore                # Ignore rules for build artifacts and temp files
├── build.bat                 # ✅ Recommended build script for Windows users
├── build.sh                  # Alternative build script for Git Bash users
├── global.json               # .NET SDK version pinning
├── LICENSE                   # MIT License
├── README.md                 # This file
│
├── RScript/                  # 🔧 Revit-side add-in for executing scripts
│   └── RScript.Addin.csproj
│
├── RScript.Addin.addin       # 📄 Manifest file for loading the RScript add-in in Revit
│
├── rscript-bridge/           # 🔌 Communication layer between VS Code and Revit
│
├── rscript-extension/        # 🧠 VS Code extension source
│
├── RScript.Combiner/         # 🧵 Script combiner engine
│
├── RScriptWorkspace/         # (Deprecated) Placeholder — real workspaces are generated dynamically
```

---

## 🛠 Optional: Extension Build

To repackage the VS Code extension manually:

```
cd rscript-extension
npm install
npm run package
```

This will generate a `.vsix` file for local use or marketplace publishing.

---

## 👥 Developer vs User Workflow

| Role               | Workflow                                                                 |
|--------------------|--------------------------------------------------------------------------|
| **User**           | Clone → `./build.bat` → Open empty folder in VS Code → Initialize → Run |
| **Extension Dev**  | Edit `extension.ts` → Press `F5` → Test changes live                    |

---

## 📄 License

Licensed under the MIT License.  
Free for personal and commercial use.

---

## 👤 Author

**Seyoum Hagos**  
Architect · Developer · Workflow Designer  
_Built in collaboration with Copilot_