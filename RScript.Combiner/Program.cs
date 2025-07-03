using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// Parse arguments
var rootPath = args.FirstOrDefault() ?? Directory.GetCurrentDirectory();
var scriptsDir = Path.Combine(rootPath, "Scripts");
var entryFileName = args.Skip(1).FirstOrDefault() ?? "Main.cs";
var entryPath = Path.Combine(scriptsDir, entryFileName);
var outputPathArg = args.Skip(2).FirstOrDefault();

// Validate entry file
if (!File.Exists(entryPath))
{
    Console.Error.WriteLine($"❌ Entry script '{entryFileName}' not found in Scripts/");
    return;
}

// Prepare output path
string combinedPath;
if (!string.IsNullOrWhiteSpace(outputPathArg))
{
    combinedPath = Path.GetFullPath(outputPathArg);
    Directory.CreateDirectory(Path.GetDirectoryName(combinedPath)!);
}
else
{
    var fallbackTemp = Path.Combine(rootPath, "Temp");
    Directory.CreateDirectory(fallbackTemp);
    combinedPath = Path.Combine(fallbackTemp, "CombinedScript.cs");
}

// Gather all non-entry script files
var allScripts = Directory.GetFiles(scriptsDir, "*.cs")
    .Where(f => !f.EndsWith(".Designer.cs") && Path.GetFileName(f) != entryFileName)
    .ToArray();

// Parse entry script and collect top-level statements
var entryText = File.ReadAllText(entryPath);
var entryTree = CSharpSyntaxTree.ParseText(entryText);
var entryRoot = (CompilationUnitSyntax)entryTree.GetRoot();

var entryTopLevel = entryRoot.Members
    .OfType<GlobalStatementSyntax>()
    .Select(n => n.ToFullString())
    .ToList();

var referencedTypeNames = new HashSet<string>(
    entryRoot.DescendantNodes()
        .OfType<IdentifierNameSyntax>()
        .Select(n => n.Identifier.Text)
);

var usingSet = new HashSet<string>
{
    "using System;",
    "using System.Collections.Generic;",
    "using System.Linq;",
    "using Autodesk.Revit.DB;"
};

usingSet.UnionWith(
    entryRoot.Usings.Select(u => u.ToFullString().Trim())
);

// Include type definitions from referenced files
var includedDefinitions = new List<string>();
foreach (var file in allScripts)
{
    var fileText = File.ReadAllText(file);
    var tree = CSharpSyntaxTree.ParseText(fileText);
    var root = (CompilationUnitSyntax)tree.GetRoot();

    var declaredTypes = root.Members.OfType<BaseTypeDeclarationSyntax>();
    var matchFound = false;

    foreach (var type in declaredTypes)
    {
        if (referencedTypeNames.Contains(type.Identifier.Text))
        {
            matchFound = true;
            includedDefinitions.Add(type.ToFullString());
        }
    }

    if (matchFound)
    {
        foreach (var u in root.Usings)
            usingSet.Add(u.ToFullString().Trim());
    }
}

// Build final script content
var output = new StringBuilder();
output.AppendLine("// ------------------------------");
output.AppendLine("// 🔧 Auto-generated CombinedScript.cs");
output.AppendLine("// ------------------------------");
output.AppendLine();

foreach (var u in usingSet.OrderBy(u => u))
    output.AppendLine(u);

output.AppendLine();

output.AppendLine("public static class CombinedEntryPoint");
output.AppendLine("{");

// 👉 Global holders
output.AppendLine("    private static Autodesk.Revit.DB.Document? StoredDoc;");
output.AppendLine("    private static RScript.Addin.Services.ScriptGlobals? Globals;");

// 👉 Transact support
output.AppendLine("    public static void Transact(string name, Action<Autodesk.Revit.DB.Document> action)");
output.AppendLine("    {");
output.AppendLine("        RScript.Addin.Services.Tx.TransactWithDoc(StoredDoc!, name, action);");
output.AppendLine("    }");

// 👉 Print via named pipe
output.AppendLine("    public static void Print(string message)");
output.AppendLine("    {");
output.AppendLine("        RScript.Addin.Services.ScriptGlobals.Print(message);");
output.AppendLine("    }");

// 👉 Global accessors from injected instance
output.AppendLine("    public static Autodesk.Revit.UI.UIDocument UIDoc => Globals!.UIDoc!;");
output.AppendLine("    public static Autodesk.Revit.UI.UIApplication UIApp => Globals!.UIApp!;");
output.AppendLine("    public static Autodesk.Revit.DB.Document Doc => StoredDoc!;");
output.AppendLine();

// 👉 Entry point for script execution
output.AppendLine("    public static void Execute(Autodesk.Revit.DB.Document doc, RScript.Addin.Services.ScriptGlobals globals)");
output.AppendLine("    {");
output.AppendLine("        StoredDoc = doc;");
output.AppendLine("        Globals = globals;");
output.AppendLine();

// 👉 Inject user top-level statements
foreach (var line in entryTopLevel)
{
    foreach (var codeLine in line.Split('\n'))
        output.AppendLine("        " + codeLine.TrimEnd());
}

output.AppendLine("    }"); // Close Execute

output.AppendLine(); // Spacer

// 👉 Inject type definitions from helper files
foreach (var block in includedDefinitions)
{
    foreach (var line in block.Split('\n'))
        output.AppendLine("    " + line.TrimEnd());
}

output.AppendLine("}"); // Close CombinedEntryPoint

// Write output
File.WriteAllText(combinedPath, output.ToString());
Console.WriteLine($"✅ CombinedScript.cs written: {combinedPath}");

// Optional debug log
if (string.IsNullOrWhiteSpace(outputPathArg))
{
    var debugLog = $"""
    🔧 RScripting Debug Log — CombinedScript.cs

    📌 Entry File:       {entryFileName}
    📂 Scripts Folder:   {scriptsDir}
    📄 Referenced Types: {string.Join(", ", referencedTypeNames.OrderBy(x => x))}

    📑 Included Files:
    {string.Join("\n", includedDefinitions.Select(s => "• " + s.Split('\n')[0].Trim()))}

    ✅ Output: {combinedPath}
    """;

    var debugPath = Path.Combine(Path.GetDirectoryName(combinedPath)!, "CombinedScript.debug.txt");
    File.WriteAllText(debugPath, debugLog.Trim());
}