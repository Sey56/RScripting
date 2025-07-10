using Autodesk.Revit.UI;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Scripting;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace RScript.Addin.Services
{
    public static class CodeRunner
    {
        public static ExecutionResult ExecuteCode(string userCode, UIApplication uiApp)
        {
            var alc = new AssemblyLoadContext("RevitScript", true);
            string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "CodeRunnerDebug.txt");
            File.WriteAllText(logPath, $"[{DateTime.Now}] Starting CodeRunner.ExecuteCode\n");

            try
            {
                // 🧭 Setup globals
                ScriptGlobals.UIApp = uiApp;
                ScriptGlobals.UIDoc = uiApp.ActiveUIDocument;
                ScriptGlobals.Doc = uiApp.ActiveUIDocument?.Document;
                ScriptGlobals.PrintLogs.Clear();

                if (ScriptGlobals.Doc == null || ScriptGlobals.UIDoc == null)
                {
                    string error = "Active document is not available. Please open a Revit document.";
                    File.AppendAllText(logPath, error + "\n");
                    return new ExecutionResult { IsSuccess = false, ErrorMessage = error };
                }

                // 📂 Load Revit DLLs
                string revitInstallPath = @"C:\Program Files\Autodesk\Revit 2025";
                if (!Directory.Exists(revitInstallPath))
                {
                    string msg = $"Revit installation directory not found at {revitInstallPath}.";
                    File.AppendAllText(logPath, msg + "\n");
                    return new ExecutionResult { IsSuccess = false, ErrorMessage = msg };
                }

                var revitDllPaths = Directory.GetFiles(revitInstallPath, "RevitAPI*.dll", SearchOption.TopDirectoryOnly);
                var revitDlls = revitDllPaths
                    .Where(IsManagedAssembly)
                    .Select(dll => MetadataReference.CreateFromFile(dll))
                    .ToList();

                if (!revitDlls.Any())
                {
                    string msg = $"No managed Revit DLLs found in {revitInstallPath}";
                    File.AppendAllText(logPath, msg + "\n");
                    return new ExecutionResult { IsSuccess = false, ErrorMessage = msg };
                }

                // ⚙️ Setup script options
                var references = new List<MetadataReference>
                {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Assembly).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Math).Assembly.Location),
                    MetadataReference.CreateFromFile(Assembly.GetExecutingAssembly().Location)
                };

                var options = ScriptOptions.Default
                    .WithReferences(references)
                    .AddReferences(revitDlls)
                    .WithImports(
                        "System",
                        "System.Linq",
                        "System.Collections.Generic",
                        "Autodesk.Revit.DB",
                        "Autodesk.Revit.UI",
                        "RScript.Addin.Services"
                    );

                // 📦 Deserialize user payload
                var scriptFiles = JsonSerializer.Deserialize<List<ScriptFile>>(userCode);
                if (scriptFiles == null || scriptFiles.Count == 0)
                {
                    string msg = "No script files received or failed to deserialize.";
                    File.AppendAllText(logPath, msg + "\n");
                    return new ExecutionResult { IsSuccess = false, ErrorMessage = msg };
                }

                var usingSet = new HashSet<string>();
                var topLevelStatements = new List<string>();
                var userDefinedTypes = new List<string>();

                foreach (var file in scriptFiles)
                {
                    var tree = CSharpSyntaxTree.ParseText(file.Content);
                    var root = tree.GetRoot();

                    foreach (var usingDirective in root.DescendantNodes().OfType<UsingDirectiveSyntax>())
                        usingSet.Add(usingDirective.ToString());

                    if (file.FileName.Equals("Main.cs", StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (var stmt in root.DescendantNodes().OfType<GlobalStatementSyntax>())
                            topLevelStatements.Add(stmt.ToString());
                    }

                    foreach (var typeDecl in root.DescendantNodes().OfType<MemberDeclarationSyntax>()
                        .Where(n => n is ClassDeclarationSyntax or StructDeclarationSyntax or EnumDeclarationSyntax))
                    {
                        userDefinedTypes.Add(typeDecl.ToFullString());
                    }
                }

                string globalImports = @"
global using static RScript.Addin.Services.ScriptGlobals;
global using static RScript.Addin.Services.Helpers;
global using static RScript.Addin.Services.Tx;
".Trim();

                string injectedHelper = @"
public static void Transact(string name, Action<Autodesk.Revit.DB.Document> action)
{
    Tx.TransactWithDoc(ScriptGlobals.Doc, name, action);
}
".Trim();

                string combinedScript =
                    globalImports + "\n\n" +
                    string.Join("\n", usingSet) + "\n\n" +
                    string.Join("\n", userDefinedTypes) + "\n\n" +
                    injectedHelper + "\n\n" +
                    string.Join("\n", topLevelStatements);

                File.AppendAllText(logPath, $"Combined script:\n{combinedScript}\n");

                // 📜 Execute script with console capture
                var originalOut = Console.Out;
                var consoleBuffer = new StringWriter();
                Console.SetOut(consoleBuffer);

                var script = CSharpScript.Create(combinedScript, options);
                var state = script.RunAsync().Result;

                string prints = string.Join("\n", ScriptGlobals.PrintLogs);
                string consoleOutput = consoleBuffer.ToString();
                Console.SetOut(originalOut);

                string finalMessage = string.IsNullOrWhiteSpace(prints) && string.IsNullOrWhiteSpace(consoleOutput)
                    ? "Code executed successfully"
                    : prints + "\n" + consoleOutput;

                return new ExecutionResult
                {
                    IsSuccess = true,
                    ResultMessage = finalMessage.Trim(),
                    ReturnValue = state.ReturnValue,
                    ScriptName = "Main.cs"
                };
            }
            catch (CompilationErrorException ex)
            {
                string[] details = ex.Diagnostics.Select(d => d.ToString()).ToArray();
                File.WriteAllLines(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "CodeEditorError.txt"), details);
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Compilation failed",
                    ErrorDetails = details
                };
            }
            catch (AggregateException ex)
            {
                string[] details = ex.InnerExceptions.Select(e => e.ToString()).ToArray();
                File.WriteAllLines(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "CodeEditorError.txt"), details);
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Script execution failed",
                    ErrorDetails = details
                };
            }
            catch (Exception ex)
            {
                string[] details = { ex.Message, ex.StackTrace ?? "No stack trace" };
                File.WriteAllLines(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "CodeEditorError.txt"), details);
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Runtime error: {ex.Message}",
                    ErrorDetails = details
                };
            }
            finally
            {
                alc.Unload();
            }
        }

        private static bool IsManagedAssembly(string filePath)
        {
            try
            {
                AssemblyName.GetAssemblyName(filePath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private class ScriptFile
        {
            public string FileName { get; set; }
            public string Content { get; set; }
        }
    }
}