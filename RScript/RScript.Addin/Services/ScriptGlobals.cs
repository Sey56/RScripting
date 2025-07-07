using Autodesk.Revit.UI;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;

namespace RScript.Addin.Services
{
    public class ScriptGlobals
    {
        public UIApplication? UIApp { get; set; }
        public UIDocument? UIDoc { get; set; }
        public Autodesk.Revit.DB.Document? Doc { get; set; }

        // Output pipe name injected before script execution
        public static string? OutputPipeName { get; set; }

        public static void Print(string message)
        {
            if (string.IsNullOrEmpty(OutputPipeName)) return;

            try
            {
                using var client = new NamedPipeClientStream(".", OutputPipeName, PipeDirection.Out);
                client.Connect(1000); // Try for 1 second
                using var writer = new StreamWriter(client, Encoding.UTF8) { AutoFlush = true };
                writer.WriteLine(message);
            }
            catch
            {
                // Silent fail – don't break script if pipe can't be written
            }
        }

        public static string GetRevitInstallDirectory()
        {
            return Path.GetDirectoryName(Environment.ProcessPath)
                ?? throw new InvalidOperationException("Could not determine Revit install path.");
        }
    }
}