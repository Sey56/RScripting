# 🚀 Getting Started with RScripting

Welcome to RScripting — a lightweight, developer-first framework for live C# scripting in Revit 2025. This guide walks you through setting up the environment, creating your own scripting workspace, and running your first script using Roslyn scripting.

---

## 🧩 Prerequisites

Make sure the following tools are installed:

| Tool                | Version     |
|---------------------|-------------|
| Windows             | 10 or 11    |
| Revit               | 2025        |
| .NET SDK            | 8.0+        |
| Node.js + npm       | Latest LTS  |
| Visual Studio Code  | Latest      |
| Git for Windows     | ✅ Required (includes Git Bash)

---

## 📥 Step 1: Clone the Repository

Open **Git Bash** and run:

```bash
git clone https://github.com/Sey56/RScripting.git
cd RScripting
```

---

## 🛠 Step 2: Build the Tooling

> ⚠️ Make sure **Revit and VS Code are closed** before running this script.

In Git Bash:

```bash
./build.sh
```

This script will:

- Compile and deploy the Revit add-in
- Build the communication bridge
- Package and install the VS Code extension

If you see:

```
✅ Extension installed
```

—you're good to go.

---

## 🗂 Step 3: Create a Scripting Workspace

Create a folder anywhere with any name:

```bash
mkdir TestWorkspace
cd TestWorkspace
code .
```

This opens your folder in VS Code. Do **not** open the RScripting repo itself.

---

## ⚙️ Step 4: Initialize Workspace in VS Code

In VS Code, open the Command Palette (`Ctrl + Shift + P`) and run:

```
RScript: Initialize Workspace
```

When prompted, click **Restore** to scaffold your workspace.

This creates:

- `Scripts/` folder with starter files
- `.csproj` targeting Revit 2025
- IntelliSense stubs for:
  - `Doc`, `UIDoc`, `UIApp`
  - `Print(...)`, `Transact(...)`
- A sample `Main.cs` file

---

## 🧵 Step 5: Start the Revit Server

- Open Revit  
- Go to the **Add-Ins** tab  
- Toggle **RScript Server** ON  
- Ensure a project/document is open

---

## ▶️ Step 6: Run Your First Script

Back in VS Code:

Open `Scripts/Main.cs`, write your code, then press:

```text
Ctrl + Alt + R
```

Or run:

```text
Ctrl + Shift + P → RScript: Send To Revit
```

---

## ✅ See the Results

Output appears in:

- **VS Code Output panel** (channel: `RScript`)
- **Revit toast overlay**
- **Local logs** (in your user folder):
  - `CodeEditorError.txt`
  - `RScriptBridgeLog.txt`
  - `CodeRunnerDebug.txt`

Each execution also generates a file like:

```
C:\Users\<your-name>\AppData\Local\Temp\RScript_CombinedScript.cs
```

Useful for debugging or traceability.

---

## 🧼 Iterate Freely

- Add `.cs` files to `Scripts/`
- Use custom types and classes
- Wrap model edits inside `Transact(...)`
- Log messages with `Print(...)`

No compiling. No packaging. Just run and go.

---

## ❓ Troubleshooting

If something doesn’t work:

- Make sure Revit 2025 is running with a project open
- Ensure the **RScript Server** toggle is ON
- Check VS Code’s `Output` panel
- Verify your Git Bash environment
- Clean out old temp files from:

```
C:\Users\<your-name>\AppData\Local\Temp
```

Look for files starting with `RScript_`.

---

Built with ❤️ by [Seyoum Hagos](https://github.com/Sey56)