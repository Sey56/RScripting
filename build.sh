#!/bin/bash
set -e
echo "--- Starting RScripting Build & Deployment ---"

# 🧭 Project paths
ROOT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
RSCRIPT_ADDIN_PROJECT_DIR="$ROOT_DIR/RScript/RScript.Addin"
RSCRIPT_BRIDGE_DIR="$ROOT_DIR/rscript-bridge"
RSCRIPT_EXTENSION_DIR="$ROOT_DIR/rscript-extension"
RSCRIPT_EXTENSION_BIN_DIR="$RSCRIPT_EXTENSION_DIR/bin"
RSCRIPT_WORKSPACE_DIR="$ROOT_DIR/RScriptWorkspace"
ADDIN_FILE_NAME="RScript.Addin.addin"
BUILD_OUTPUT_DIR="$RSCRIPT_ADDIN_PROJECT_DIR/bin/Debug/net8.0-windows/win-x64"
REVT_ADDINS_TARGET="$HOME/AppData/Roaming/Autodesk/Revit/Addins/2025/RScript"

# 🛠 .NET check
DOTNET_PATH=$(command -v dotnet)
if [ -z "$DOTNET_PATH" ]; then
  echo "❌ .NET SDK not found in PATH. Install .NET 8." >&2
  exit 1
fi

# 🔨 Build rscript-bridge
echo "🔧 Building rscript-bridge..."
"$DOTNET_PATH" publish "$RSCRIPT_BRIDGE_DIR/rscript-bridge.csproj" -c Debug -o "$RSCRIPT_BRIDGE_DIR/bin/Debug/publish" --no-self-contained
echo "✅ rscript-bridge built."


# 📦 Copy rscript-bridge to VS Code extension
echo "🔧 Copying rscript-bridge to VS Code extension bin..."
mkdir -p "$RSCRIPT_EXTENSION_BIN_DIR"
cp "$RSCRIPT_BRIDGE_DIR/bin/Debug/publish"/rscript-bridge.{dll,exe,runtimeconfig.json} "$RSCRIPT_EXTENSION_BIN_DIR/"
echo "✅ rscript-bridge deployed."

# 🔨 Build RScript.Addin
echo "🔧 Building RScript.Addin..."
pushd "$RSCRIPT_ADDIN_PROJECT_DIR" > /dev/null
"$DOTNET_PATH" build -c Debug
popd > /dev/null
echo "✅ RScript.Addin built."

# 🔨 Build RScript.Combiner
echo "🔧 Building RScript.Combiner..."
"$DOTNET_PATH" build "$ROOT_DIR/RScript.Combiner/RScript.Combiner.csproj" -c Debug
echo "✅ RScript.Combiner built."

# 📦 Copy RScript.Combiner files to VS Code extension
COMBINER_BUILD_DIR="$ROOT_DIR/RScript.Combiner/bin/Debug/net8.0-windows/win-x64"
echo "🔧 Copying RScript.Combiner to VS Code extension bin..."
cp "$COMBINER_BUILD_DIR"/RScript.Combiner.{exe,dll,runtimeconfig.json} "$RSCRIPT_EXTENSION_BIN_DIR/"
cp "$COMBINER_BUILD_DIR"/{Microsoft.CodeAnalysis.dll,Microsoft.CodeAnalysis.CSharp.dll,System.Collections.Immutable.dll} "$RSCRIPT_EXTENSION_BIN_DIR/"
echo "✅ RScript.Combiner deployed."

# 📂 Deploy add-in DLLs
echo "🗂 Deploying RScript.Addin to Revit Addins folder..."
mkdir -p "$REVT_ADDINS_TARGET"
cp "$BUILD_OUTPUT_DIR"/* "$REVT_ADDINS_TARGET/"

# 🔐 Deploy .addin manifest to root Addins folder
ADDIN_DEST="$HOME/AppData/Roaming/Autodesk/Revit/Addins/2025/RScript.Addin.addin"
if [ -f "$ROOT_DIR/$ADDIN_FILE_NAME" ]; then
  cp "$ROOT_DIR/$ADDIN_FILE_NAME" "$ADDIN_DEST"
  echo "✅ Addin manifest copied to Revit Addins root."
else
  echo "⚠️  Warning: $ADDIN_FILE_NAME not found at repo root."
fi

# 🧪 Package VS Code extension
echo "📦 Packaging VS Code extension..."
pushd "$RSCRIPT_EXTENSION_DIR" > /dev/null
npm install
yes | vsce package
popd > /dev/null

VSIX_FILE=$(find "$RSCRIPT_EXTENSION_DIR" -name "rscript-extension-*.vsix" | head -n 1)
if [ -z "$VSIX_FILE" ]; then
  echo "❌ Could not find packaged .vsix file." >&2
  exit 1
fi
echo "✅ Extension packaged: $VSIX_FILE"

# 🔁 Uninstall and reinstall extension
echo "🔁 Installing VS Code extension..."
EXTENSION_ID=$(node -e "console.log(require('./rscript-extension/package.json').publisher + '.' + require('./rscript-extension/package.json').name)")
code --uninstall-extension "$EXTENSION_ID" || echo "ℹ️ Previous extension not found or skipped."
code --install-extension "$VSIX_FILE"
echo "✅ Extension installed."

# 🧹 Clean Workspace
echo "🧽 Cleaning workspace directory..."
mkdir -p "$RSCRIPT_WORKSPACE_DIR"
find "$RSCRIPT_WORKSPACE_DIR" -mindepth 1 -delete
echo "✅ Workspace ready: $RSCRIPT_WORKSPACE_DIR"

echo "--- ✅ Build & Deployment Complete ---"
echo "
🚀 Next steps:
1. Restart VS Code if open.
2. Open workspace: $RSCRIPT_WORKSPACE_DIR
3. Run: 'RevitScripting: Initialize Workspace' from VS Code Command Palette.
4. Launch Revit — the RScript Add-in should be loaded and ready."