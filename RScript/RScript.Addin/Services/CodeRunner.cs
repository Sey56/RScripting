using Autodesk.Revit.UI;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;

namespace RScript.Addin.Services
{
    public static class CodeRunner
    {
        public static ExecutionResult ExecuteCode(string userCode, UIApplication uiApp)
        {
            var alc = new AssemblyLoadContext("RevitScript", isCollectible: true);
            string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "CodeRunnerDebug.txt");
            File.WriteAllText(logPath, $"🔧 Executing script: {DateTime.Now}\n");

            try
            {
                var globals = new ScriptGlobals
                {
                    UIApp = uiApp,
                    UIDoc = uiApp.ActiveUIDocument,
                    Doc = uiApp.ActiveUIDocument.Document
                };

                if (globals.Doc == null)
                {
                    File.AppendAllText(logPath, "❌ Document is null in script globals\n");
                    throw new InvalidOperationException("Document is null in script globals");
                }

                string revitInstallPath = ScriptGlobals.GetRevitInstallDirectory();
                if (!Directory.Exists(revitInstallPath))
                {
                    string msg = $"❌ Revit installation directory not found at {revitInstallPath}";
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
                    string msg = $"❌ No managed Revit DLLs found in {revitInstallPath}";
                    File.AppendAllText(logPath, msg + "\n");
                    return new ExecutionResult { IsSuccess = false, ErrorMessage = msg };
                }

                var references = new List<MetadataReference>
                {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Assembly).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Math).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.Console).Assembly.Location),
                    MetadataReference.CreateFromFile(Assembly.GetExecutingAssembly().Location)
                };
                references.AddRange(revitDlls);

                // 🔧 Add system references explicitly (mimicking ScriptOptions.Default behavior)
                var coreDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
                var systemAssemblies = new[]
{
    "System.dll",
    "System.Core.dll",
    "System.Runtime.dll",
    "System.Private.CoreLib.dll",
    "System.Console.dll",
    "System.Collections.dll",
    "System.Linq.dll",
    "System.Threading.Tasks.dll",
    "System.Private.Uri.dll",
    "System.Net.Http.dll",
    "netstandard.dll"
};

                foreach (var name in systemAssemblies)
                {
                    var path = Path.Combine(coreDir, name);
                    if (File.Exists(path))
                    {
                        references.Add(MetadataReference.CreateFromFile(path));
                        File.AppendAllText(logPath, $"✅ Found and added: {name}\n");
                    }
                    else
                    {
                        File.AppendAllText(logPath, $"❌ Missing: {name} at {path}\n");
                    }
                }

                var syntaxTree = CSharpSyntaxTree.ParseText(userCode);
                var compilation = CSharpCompilation.Create("RScriptUserAssembly")
                    .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                    .AddReferences(references)
                    .AddSyntaxTrees(syntaxTree);

                using var ms = new MemoryStream();
                var emitResult = compilation.Emit(ms);

                if (!emitResult.Success)
                {
                    var errors = emitResult.Diagnostics
                        .Where(d => d.Severity == DiagnosticSeverity.Error)
                        .Select(d => d.ToString());

                    string errorMessages = "🛑 Compilation failed:\n" + string.Join("\n", errors);
                    File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "CodeEditorError.txt"), errorMessages);
                    File.AppendAllText(logPath, errorMessages + "\n");
                    ScriptGlobals.Print(errorMessages);
                    return new ExecutionResult { IsSuccess = false, ErrorMessage = errorMessages };
                }

                ms.Seek(0, SeekOrigin.Begin);
                var assembly = alc.LoadFromStream(ms);
                var entryType = assembly.GetType("CombinedEntryPoint") ?? throw new Exception("Entry point type 'CombinedEntryPoint' not found.");
                var method = entryType.GetMethod("Execute", BindingFlags.Public | BindingFlags.Static) ?? throw new Exception("Static method 'Execute(Document)' not found in 'CombinedEntryPoint'.");
                method.Invoke(null, [globals.Doc, globals]);

                File.AppendAllText(logPath, "✅ Script executed successfully.\n");
                ScriptGlobals.Print("✅ Code executed successfully.");
                return new ExecutionResult { IsSuccess = true, ResultMessage = "Code executed successfully." };
            }
            catch (Exception ex)
            {
                string errorLogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "CodeEditorError.txt");

                // 🧠 Extract the most specific exception, including InnerException
                string coreMessage = ex.InnerException?.Message ?? ex.Message;
                string fullTrace = $"{ex}\n{ex.InnerException}";

                File.WriteAllText(errorLogPath, $"Error: {coreMessage}\n\nStack Trace:\n{fullTrace}");
                File.AppendAllText(logPath, $"💥 Runtime error:\n{coreMessage}\n\n{fullTrace}\n");

                // 🌟 Send human-readable message to VS Code output pane
                ScriptGlobals.Print($"🛑 Script failed: {coreMessage}");

                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Script failed: {coreMessage}"
                };
            }
            finally
            {
                alc.Unload();
            }
        }

        private static bool IsManagedAssembly(string filePath)
        {
            try { AssemblyName.GetAssemblyName(filePath); return true; }
            catch { return false; }
        }
    }

    public class ExecutionResult
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ResultMessage { get; set; }
        public dynamic? ReturnValue { get; set; }
    }
}