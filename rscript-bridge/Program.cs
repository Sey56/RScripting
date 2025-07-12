using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;

class Program
{
    static void Main(string[] args)
    {
        string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "RScriptBridgeLog.txt");
        File.AppendAllText(logPath, $"Bridge started at {DateTime.Now}\n");

        try
        {
            if (args.Length == 0)
            {
                Console.WriteLine("[ERROR] No script folder path provided.");
                File.AppendAllText(logPath, "No script folder path provided.\n");
                Environment.Exit(1);
            }

            string scriptsFolder = args[0];
            if (!Directory.Exists(scriptsFolder))
            {
                Console.WriteLine($"[ERROR] Scripts folder not found: {scriptsFolder}");
                File.AppendAllText(logPath, $"Scripts folder not found: {scriptsFolder}\n");
                Environment.Exit(1);
            }

            var scriptFiles = Directory.GetFiles(scriptsFolder, "*.cs");
            var scriptList = new List<object>();

            foreach (var file in scriptFiles)
            {
                string content = File.ReadAllText(file);
                scriptList.Add(new
                {
                    FileName = Path.GetFileName(file),
                    Content = content
                });
            }

            string jsonPayload = JsonSerializer.Serialize(scriptList);
            byte[] payloadBytes = Encoding.UTF8.GetBytes(jsonPayload);

            using var pipeClient = new NamedPipeClientStream(".", "RScriptPipe", PipeDirection.InOut, PipeOptions.Asynchronous);
            try
            {
                pipeClient.Connect(1000); // 1 second timeout
            }
            catch (TimeoutException)
            {
                Console.WriteLine("[ERROR] RScriptServer not running — please start it from the Revit Add-ins tab.");
                File.AppendAllText(logPath, "Connection timeout: RScriptServer likely not running.\n");
                Environment.Exit(2); // Specific exit code for server unavailable
            }

            File.AppendAllText(logPath, "Connected to pipe server.\n");

            // Write payload length
            byte[] lengthBytes = BitConverter.GetBytes(payloadBytes.Length);
            pipeClient.Write(lengthBytes, 0, 4);
            pipeClient.Flush();

            // Write payload
            pipeClient.Write(payloadBytes, 0, payloadBytes.Length);
            pipeClient.Flush();
            File.AppendAllText(logPath, $"Sent payload of {payloadBytes.Length} bytes.\n");

            // Read response length
            byte[] responseLengthBuffer = new byte[4];
            pipeClient.Read(responseLengthBuffer, 0, 4);
            int responseLength = BitConverter.ToInt32(responseLengthBuffer, 0);

            // Read response
            byte[] responseBuffer = new byte[responseLength];
            int bytesRead = 0;
            while (bytesRead < responseLength)
            {
                int read = pipeClient.Read(responseBuffer, bytesRead, responseLength - bytesRead);
                if (read == 0) break;
                bytesRead += read;
            }

            string response = Encoding.UTF8.GetString(responseBuffer);
            Console.WriteLine(response);
            File.AppendAllText(logPath, $"Received response: {response}\n");

            File.AppendAllText(logPath, "Bridge exiting cleanly.\n");
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Console.WriteLine("[ERROR] Unexpected bridge failure. Make sure RScriptServer is running.");
            Console.WriteLine($"Details: {ex.Message}");
            File.AppendAllText(logPath, $"Bridge error: {ex.Message}\n");
            Environment.Exit(3); // General catch-all error
        }
    }
}