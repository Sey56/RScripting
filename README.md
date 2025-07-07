🏗️ RScripting
Live C# Scripting for Revit 2025Powered by Roslyn, VS Code, and .NET 8.0
RScripting transforms Revit automation by connecting Visual Studio Code to Revit 2025. Write and run C# scripts with full IntelliSense and Revit API access—no compiling or add-in builds needed.

🚀 Why RScripting?

🔁 Instant Execution: Run C# scripts in Revit with one keystroke.
🧠 Modern .NET 8.0: Use the latest C# features without legacy constraints.
🪶 Lightweight: No binaries, AppDomains, or complex setup.
🌐 Revit Context: Access Doc, UIDoc, UIApp, Print(), and Transact().
📢 Clear Feedback: Errors and logs in Revit’s toast overlay and VS Code.
🧪 Safe Transactions: Script-scoped changes for secure edits.
📦 Portable: Share .cs scripts easily via zip or version control.


🛠 Build Options
Choose one of two equivalent build methods:
✅ Windows Command Line (Recommended)
./build.bat


Runs in Command Prompt or PowerShell
No Git Bash or Unix tools required
Compiles, deploys Revit add-in, and installs VS Code extension

🐧 Git Bash (Alternative)
./build.sh


Uses Git Bash (from Git for Windows)
Suits Unix-style scripting fans


Tip: Both scripts do the same thing—pick what matches your setup.


💻 Requirements



Component
Version



Windows
10 or 11


Revit
2025


.NET SDK
8.0+


Node.js + npm
Latest LTS


Visual Studio Code
Latest


Git (optional)
For cloning



🖲️ Revit Integration
Revit loads the RScript Add-In with a single button:
🔘 RScript Server

Activates: A lightweight server in Revit.
Listens: For script requests from VS Code.
Enables: Live script execution.


Note: This button links your VS Code workspace to Revit for real-time automation.


🧪 Quick Start

Open VS Code  

Open an empty folder (the extension uses this as the workspace).


Initialize Workspace  

Run: Command Palette → RScript: Initialize Workspace  
Creates:
Scripts/, Stubs/, Tools/ folders
.csproj with Revit API references
IntelliSense stubs (Doc, UIDoc, etc.)
Sample Main.cs and transaction helpers
global.json for .NET SDK version




Run Scripts  

Shortcut: Ctrl + Alt + R  
Or: Command Palette → RScript: Run Script  
Output in:
Revit’s toast overlay
VS Code’s “RScripting” Output panel
Local log files






Tip: All messages and errors use Print(...) for clear feedback.


🧠 How It Works

🔹 RScript Add-In  

Loaded via RScript.Addin.addin.  
Adds RScript Server button to Revit.  
Executes scripts using Roslyn.


🔹 VS Code Extension  

Commands: Initialize Workspace, Run Script.  
Sets up .csproj and Scripts/Main.cs.


🔹 Main.cs  

Your script entry point for:  
using statements  
Core logic  
Custom types


Reference other .cs files in Scripts/.


🔹 Combiner  

Merges Main.cs and referenced scripts into CombinedScript.cs.  
Saves to temp directory for debugging.


🔹 Bridge  

Sends scripts to Revit via named pipe/socket.  
Streams results to VS Code, Revit, and logs.




🧩 Project Structure
RScripting/
├── .git/                     # Git metadata
├── .gitignore                # Ignores build artifacts
├── build.bat                 # Windows build script
├── build.sh                  # Git Bash build script
├── global.json               # .NET SDK version
├── LICENSE                   # MIT License
├── README.md                 # This file
│
├── RScript/                  # Revit add-in
│   └── RScript.Addin.csproj
├── RScript.Addin.addin       # Revit manifest
├── rscript-bridge/           # VS Code-Revit link
├── rscript-extension/        # VS Code extension
├── RScript.Combiner/         # Script combiner
├── RScriptWorkspace/         # Deprecated placeholder


🛠 Build Extension (Optional)
cd rscript-extension
npm install
npm run package


Creates a .vsix file for local or marketplace use.


👥 Workflows



Role
Steps



User
Clone repo → Run ./build.bat → Open VS Code → Initialize → Run scripts


Developer
Edit extension.ts → Press F5 → Test changes live



📄 License
MIT LicenseFree for personal and commercial use.

👤 Author
Seyoum HagosArchitect · Developer · Workflow DesignerBuilt with Copilot