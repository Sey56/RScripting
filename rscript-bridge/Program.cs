using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Please provide a .cs file path as an argument.");
            return;
        }

        string scriptPath = args[0];
        if (!File.Exists(scriptPath))
        {
            Console.WriteLine($"File not found: {scriptPath}");
            return;
        }

        string scriptContent = File.ReadAllText(scriptPath);
        byte[] scriptBytes = Encoding.UTF8.GetBytes(scriptContent);

        // 🔁 Generate unique output pipe name
        string outputPipeName = $"RScriptOutputPipe_{Guid.NewGuid():N}";
        byte[] outputPipeBytes = Encoding.UTF8.GetBytes(outputPipeName);

        // 🔊 Prepare output pipe server and bind early
        var outputPipe = new NamedPipeServerStream(outputPipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        var outputReader = new StreamReader(outputPipe);

        // ⏳ Begin listening BEFORE sending to Revit
        var outputThread = new Thread(() =>
        {
            try
            {
                outputPipe.WaitForConnection();
                string line;
                while ((line = outputReader.ReadLine()) != null)
                {
                    Console.WriteLine("[Revit] " + line);
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"[OutputPipe Error] {ex.Message}");
            }
        });
        outputThread.Start();

        // 📤 Connect to RevitScriptServer pipe and send script + pipe name
        using var pipeClient = new NamedPipeClientStream(".", "RScriptPipe", PipeDirection.Out);
        pipeClient.Connect(5000);
        using var writer = new BinaryWriter(pipeClient);

        writer.Write(scriptBytes.Length);
        writer.Write(scriptBytes);
        writer.Write(outputPipeBytes.Length);
        writer.Write(outputPipeBytes);
        writer.Flush();

        Console.WriteLine("[Listener] Waiting for Revit to stream output...");
        outputThread.Join(10000); // Wait up to 10s for listener to complete

        outputPipe.Dispose();
    }
}