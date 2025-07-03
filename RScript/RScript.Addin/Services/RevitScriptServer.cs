using Autodesk.Revit.UI;
using RScript.Addin.App;
using RScript.Addin.ViewModels;
using System.IO;
using System.IO.Pipes;
using System.Text;

namespace RScript.Addin.Services
{
    public class RevitScriptServer
    {
        private bool _running;
        private readonly string _logPath = Path.Combine(RevitExternalApp.HomePath, "RScriptServerLog.txt");
        private NamedPipeServerStream _pipeServer;
        private CancellationTokenSource _cts;
        private readonly UIApplication _uiApp;

        public RevitScriptServer(UIApplication uiApp)
        {
            _uiApp = uiApp;
            _running = false;
            File.WriteAllText(_logPath, "Server initialized: " + DateTime.Now + "\n");
            _cts = new CancellationTokenSource();
        }

        public void Start()
        {
            if (_running) return;
            _running = true;
            _cts = new CancellationTokenSource();
            File.WriteAllText(_logPath, "Server started: " + DateTime.Now + "\n");
            ListenForScriptsAsync(_cts.Token);
        }

        private async void ListenForScriptsAsync(CancellationToken cancellationToken)
        {
            while (_running && !cancellationToken.IsCancellationRequested)
            {
                _pipeServer?.Dispose();
                _pipeServer = new NamedPipeServerStream("RScriptPipe", PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

                File.AppendAllText(_logPath, "Waiting for connection: " + DateTime.Now + "\n");
                try
                {
                    await _pipeServer.WaitForConnectionAsync(cancellationToken);
                    if (cancellationToken.IsCancellationRequested)
                    {
                        File.AppendAllText(_logPath, "Connection wait cancelled: " + DateTime.Now + "\n");
                        break;
                    }

                    using var reader = new BinaryReader(_pipeServer, Encoding.UTF8, leaveOpen: true);
                    using var writer = new StreamWriter(_pipeServer, leaveOpen: true) { AutoFlush = true };

                    int length = reader.ReadInt32();
                    byte[] scriptBytes = reader.ReadBytes(length);
                    int outputPipeLength = reader.ReadInt32();
                    byte[] outputPipeBytes = reader.ReadBytes(outputPipeLength);
                    string outputPipeName = Encoding.UTF8.GetString(outputPipeBytes);
                    ScriptGlobals.OutputPipeName = outputPipeName;

                    File.AppendAllText(_logPath, $"Received output pipe name: {outputPipeName} - {DateTime.Now}\n");
                    string scriptContent = Encoding.UTF8.GetString(scriptBytes);
                    File.AppendAllText(_logPath, $"Received script: {scriptContent} - {DateTime.Now}\n");

                    if (string.IsNullOrEmpty(scriptContent))
                    {
                        File.AppendAllText(_logPath, "Empty script received: " + DateTime.Now + "\n");
                        await writer.WriteLineAsync("Error: Empty script content.");
                    }
                    else
                    {
                        var result = MainViewModel.Instance.QueueScriptFromServer(scriptContent, _uiApp);
                        File.AppendAllText(_logPath, "Script execution queued: " + DateTime.Now + "\n");

                        if (result.IsSuccess)
                        {
                            await writer.WriteLineAsync(result.ResultMessage ?? "Script queued successfully.");
                        }
                        else
                        {
                            await writer.WriteLineAsync($"Error: {result.ErrorMessage}");
                        }
                    }

                    await writer.FlushAsync();
                    File.AppendAllText(_logPath, "Pipe disconnected: " + DateTime.Now + "\n");
                    _pipeServer.Disconnect();
                }
                catch (OperationCanceledException)
                {
                    File.AppendAllText(_logPath, "Connection wait cancelled: " + DateTime.Now + "\n");
                    break;
                }
                catch (Exception ex)
                {
                    File.AppendAllText(_logPath, "Error in ListenForScriptsAsync: " + ex.Message + " - " + DateTime.Now + "\n");
                    if (_pipeServer.IsConnected)
                    {
                        _pipeServer.Disconnect();
                    }
                }
            }
            File.AppendAllText(_logPath, "ListenForScriptsAsync stopped: " + DateTime.Now + "\n");

            if (_pipeServer != null && _pipeServer.IsConnected)
            {
                _pipeServer.Disconnect();
            }
            _pipeServer?.Dispose();
        }

        public void Stop()
        {
            if (!_running) return;
            _running = false;
            File.AppendAllText(_logPath, "Server stopping: " + DateTime.Now + "\n");

            _cts?.Cancel();

            try
            {
                Task.Delay(100).Wait();

                if (_pipeServer != null && _pipeServer.IsConnected)
                {
                    _pipeServer.Disconnect();
                }

                _pipeServer?.Dispose();
                _pipeServer = null;
            }
            catch (Exception ex)
            {
                File.AppendAllText(_logPath, "Error stopping server: " + ex.Message + " - " + DateTime.Now + "\n");
            }

            _cts?.Dispose();
            _cts = null;

            File.AppendAllText(_logPath, "Server stopped: " + DateTime.Now + "\n");
        }

        public bool IsRunning() => _running;
    }
}