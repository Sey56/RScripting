using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;

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
                Console.WriteLine("Please provide the path to the Scripts folder.");
                File.AppendAllText(logPath, "No script folder path provided.\n");
                Environment.Exit(1);
            }

            string scriptsFolder = args[0];
            if (!Directory.Exists(scriptsFolder))
            {
                Console.WriteLine($"Scripts folder not found: {scriptsFolder}");
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
            pipeClient.Connect(5000);
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
        }
        catch (TimeoutException)
        {
            Console.WriteLine("Connection to Revit timed out. Make sure RScript is running in Revit.");
            File.AppendAllText(logPath, "Connection timeout error\n");
            Environment.Exit(1);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending scripts: {ex.Message}");
            File.AppendAllText(logPath, $"Bridge error: {ex.Message}\n");
            Environment.Exit(1);
        }


        // Ensure process exits
        Environment.Exit(0);
    }
}