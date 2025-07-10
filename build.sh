#!/bin/bash
set -e
echo "--- Starting RScripting Build & Deployment ---"

# 🧭 Project paths
ROOT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
RSCRIPT_ADDIN_PROJECT_DIR="$ROOT_DIR/RScript/RScript.Addin"
RSCRIPT_BRIDGE_DIR="$ROOT_DIR/rscript-bridge"
RSCRIPT_EXTENSION_DIR="$ROOT_DIR/rscript-extension"
RSCRIPT_EXTENSION_BIN_DIR="$RSCRIPT_EXTENSION_DIR/bin"
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
"$DOTNET_PATH" build "$RSCRIPT_BRIDGE_DIR/rscript-bridge.csproj" -c Debug

# 📦 Copy rscript-bridge outputs to VS Code extension bin
echo "🔧 Copying rscript-bridge to VS Code extension bin..."
BRIDGE_BUILD_DIR="$RSCRIPT_BRIDGE_DIR/bin/Debug/net8.0-windows/win-x64"
mkdir -p "$RSCRIPT_EXTENSION_BIN_DIR"
cp "$BRIDGE_BUILD_DIR"/rscript-bridge.{exe,dll,runtimeconfig.json} "$RSCRIPT_EXTENSION_BIN_DIR/"
echo "✅ rscript-bridge deployed."

# 🔨 Build RScript.Addin
echo "🔧 Building RScript.Addin..."
pushd "$RSCRIPT_ADDIN_PROJECT_DIR" > /dev/null
"$DOTNET_PATH" build -c Debug
popd > /dev/null
echo "✅ RScript.Addin built."

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

echo "--- ✅ Build & Deployment Complete ---"
echo "
🚀 Next steps:
1. Restart VS Code if open.
2. Open your own empty workspace folder.
3. Run: 'RevitScripting: Initialize Workspace' from VS Code Command Palette.
4. Launch Revit — the RScript Add-in should be loaded and ready."
