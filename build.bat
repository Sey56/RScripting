@echo off
setlocal enabledelayedexpansion

echo --- Starting RScripting Build & Deployment ---

:: 🧭 Project paths
set ROOT_DIR=%~dp0
set RSCRIPT_ADDIN_PROJECT_DIR=%ROOT_DIR%RScript\RScript.Addin
set RSCRIPT_BRIDGE_DIR=%ROOT_DIR%rscript-bridge
set RSCRIPT_EXTENSION_DIR=%ROOT_DIR%rscript-extension
set RSCRIPT_EXTENSION_BIN_DIR=%RSCRIPT_EXTENSION_DIR%\bin
set RSCRIPT_WORKSPACE_DIR=%ROOT_DIR%RScriptWorkspace
set ADDIN_FILE_NAME=RScript.Addin.addin
set BUILD_OUTPUT_DIR=%RSCRIPT_ADDIN_PROJECT_DIR%\bin\Debug\net8.0-windows\win-x64
set REVT_ADDINS_TARGET=%USERPROFILE%\AppData\Roaming\Autodesk\Revit\Addins\2025\RScript

:: 🛠 .NET check
where dotnet >nul 2>nul
if errorlevel 1 (
    echo ❌ .NET SDK not found in PATH. Install .NET 8.
    exit /b 1
)

:: 🔨 Build rscript-bridge
echo 🔧 Building rscript-bridge...
dotnet publish "%RSCRIPT_BRIDGE_DIR%\rscript-bridge.csproj" -c Debug -o "%RSCRIPT_BRIDGE_DIR%\bin\Debug\publish" --no-self-contained
echo ✅ rscript-bridge built.

:: 📦 Copy rscript-bridge to VS Code extension bin
echo 🔧 Copying rscript-bridge to VS Code extension bin...
mkdir "%RSCRIPT_EXTENSION_BIN_DIR%" 2>nul
copy /Y "%RSCRIPT_BRIDGE_DIR%\bin\Debug\publish\rscript-bridge.exe" "%RSCRIPT_EXTENSION_BIN_DIR%"
copy /Y "%RSCRIPT_BRIDGE_DIR%\bin\Debug\publish\rscript-bridge.dll" "%RSCRIPT_EXTENSION_BIN_DIR%"
copy /Y "%RSCRIPT_BRIDGE_DIR%\bin\Debug\publish\runtimeconfig.json" "%RSCRIPT_EXTENSION_BIN_DIR%"
echo ✅ rscript-bridge deployed.

:: 🔨 Build RScript.Addin
echo 🔧 Building RScript.Addin...
dotnet build "%RSCRIPT_ADDIN_PROJECT_DIR%" -c Debug
echo ✅ RScript.Addin built.

:: 🔨 Build RScript.Combiner
echo 🔧 Building RScript.Combiner...
dotnet build "%ROOT_DIR%RScript.Combiner\RScript.Combiner.csproj" -c Debug
echo ✅ RScript.Combiner built.

:: 📦 Copy RScript.Combiner files to VS Code extension bin
set COMBINER_BUILD_DIR=%ROOT_DIR%RScript.Combiner\bin\Debug\net8.0-windows\win-x64
echo 🔧 Copying RScript.Combiner to VS Code extension bin...
copy /Y "%COMBINER_BUILD_DIR%\RScript.Combiner.exe" "%RSCRIPT_EXTENSION_BIN_DIR%"
copy /Y "%COMBINER_BUILD_DIR%\RScript.Combiner.dll" "%RSCRIPT_EXTENSION_BIN_DIR%"
copy /Y "%COMBINER_BUILD_DIR%\RScript.Combiner.runtimeconfig.json" "%RSCRIPT_EXTENSION_BIN_DIR%"
copy /Y "%COMBINER_BUILD_DIR%\Microsoft.CodeAnalysis.dll" "%RSCRIPT_EXTENSION_BIN_DIR%"
copy /Y "%COMBINER_BUILD_DIR%\Microsoft.CodeAnalysis.CSharp.dll" "%RSCRIPT_EXTENSION_BIN_DIR%"
copy /Y "%COMBINER_BUILD_DIR%\System.Collections.Immutable.dll" "%RSCRIPT_EXTENSION_BIN_DIR%"
echo ✅ RScript.Combiner deployed.

:: 📂 Deploy add-in DLLs
echo 🗂 Deploying RScript.Addin to Revit Addins folder...
mkdir "%REVT_ADDINS_TARGET%" 2>nul
xcopy /Y /Q "%BUILD_OUTPUT_DIR%\*" "%REVT_ADDINS_TARGET%\"
echo ✅ DLLs copied to Revit Addins folder.

:: 🔐 Deploy .addin manifest
set ADDIN_DEST=%USERPROFILE%\AppData\Roaming\Autodesk\Revit\Addins\2025\RScript.Addin.addin
if exist "%ROOT_DIR%%ADDIN_FILE_NAME%" (
    copy /Y "%ROOT_DIR%%ADDIN_FILE_NAME%" "%ADDIN_DEST%"
    echo ✅ Addin manifest copied to Revit Addins root.
) else (
    echo ⚠️  Warning: %ADDIN_FILE_NAME% not found at repo root.
)

:: 🧪 Package VS Code extension
echo 📦 Packaging VS Code extension...
pushd "%RSCRIPT_EXTENSION_DIR%"
call npm install
for /f "delims=" %%f in ('npx vsce package') do set VSIX_FILE=%%f
popd

if not exist "%VSIX_FILE%" (
    echo ❌ Could not find packaged .vsix file.
    exit /b 1
)
echo ✅ Extension packaged: %VSIX_FILE%

:: 🔁 Uninstall and reinstall extension
echo 🔁 Installing VS Code extension...
for /f "delims=" %%i in ('node -e "console.log(require('./rscript-extension/package.json').publisher + '.' + require('./rscript-extension/package.json').name)"') do set EXTENSION_ID=%%i
code --uninstall-extension %EXTENSION_ID% >nul 2>nul
code --install-extension "%VSIX_FILE%"
echo ✅ Extension installed.

:: 🧹 Clean Workspace
echo 🧽 Cleaning workspace directory...
mkdir "%RSCRIPT_WORKSPACE_DIR%" 2>nul
powershell -Command "Remove-Item -Path '%RSCRIPT_WORKSPACE_DIR%\*' -Recurse -Force -ErrorAction SilentlyContinue"
echo ✅ Workspace ready: %RSCRIPT_WORKSPACE_DIR%

echo --- ✅ Build & Deployment Complete ---
echo.
echo 🚀 Next steps:
echo 1. Restart VS Code if open.
echo 2. Open workspace: %RSCRIPT_WORKSPACE_DIR%
echo 3. Run: 'RevitScripting: Initialize Workspace' from VS Code Command Palette.
echo 4. Launch Revit — the RScript Add-in should be loaded and ready.