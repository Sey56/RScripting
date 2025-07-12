using Autodesk.Revit.UI;
using RScript.Addin.App;
using RScript.Addin.ViewModels;
using System.IO;
using System.IO.Pipes;
using System.Text;

namespace RScript.Addin.Services
{
    public class RScriptServer
    {
        private bool _running;
        private readonly string _logPath = Path.Combine(RevitExternalApp.HomePath, "RScriptServerLog.txt");
        private CancellationTokenSource _cts;
        private readonly UIApplication _uiApp;

        public RScriptServer(UIApplication uiApp)
        {
            _uiApp = uiApp;
            _running = false;
            _cts = new CancellationTokenSource();
            File.WriteAllText(_logPath, $"Server initialized: {DateTime.Now}\n");
        }

        public void Start()
        {
            if (_running) return;
            _running = true;
            _cts = new CancellationTokenSource();
            File.AppendAllText(_logPath, $"Server started: {DateTime.Now}\n");
            ListenForScriptsAsync(_cts.Token);
        }

        private async void ListenForScriptsAsync(CancellationToken cancellationToken)
        {
            while (_running && !cancellationToken.IsCancellationRequested)
            {
                File.AppendAllText(_logPath, $"Waiting for connection: {DateTime.Now}\n");

                using var pipeServer = new NamedPipeServerStream(
                    "RScriptPipe",
                    PipeDirection.InOut,
                    1,
                    PipeTransmissionMode.Message,
                    PipeOptions.Asynchronous);

                try
                {
                    await pipeServer.WaitForConnectionAsync(cancellationToken);
                    if (cancellationToken.IsCancellationRequested) break;

                    byte[] lengthBuffer = new byte[4];
                    await pipeServer.ReadAsync(lengthBuffer, 0, 4, cancellationToken);
                    int payloadLength = BitConverter.ToInt32(lengthBuffer, 0);

                    byte[] payloadBuffer = new byte[payloadLength];
                    int bytesRead = 0;
                    while (bytesRead < payloadLength)
                    {
                        int read = await pipeServer.ReadAsync(payloadBuffer, bytesRead, payloadLength - bytesRead, cancellationToken);
                        if (read == 0) break;
                        bytesRead += read;
                    }

                    string scriptContent = Encoding.UTF8.GetString(payloadBuffer);
                    File.AppendAllText(_logPath, $"Received script ({payloadLength} bytes) - {DateTime.Now}\n");

                    ExecutionResult finalResult;
                    if (string.IsNullOrWhiteSpace(scriptContent))
                    {
                        finalResult = new ExecutionResult
                        {
                            IsSuccess = false,
                            ErrorMessage = "Empty script content received."
                        };
                    }
                    else
                    {
                        var completionSource = new TaskCompletionSource<ExecutionResult>();
                        void Handler(ExecutionResult result) => completionSource.TrySetResult(result);
                        MainViewModel.Instance.OnExecutionComplete += Handler;

                        MainViewModel.Instance.QueueScriptFromServer(scriptContent, _uiApp);

                        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(45), cancellationToken);
                        var finishedTask = await Task.WhenAny(completionSource.Task, timeoutTask);

                        if (finishedTask == completionSource.Task)
                            finalResult = await completionSource.Task;
                        else
                            finalResult = new ExecutionResult { IsSuccess = false, ErrorMessage = "Execution timed out." };

                        MainViewModel.Instance.OnExecutionComplete -= Handler;
                    }

                    string response = finalResult.IsSuccess
    ? finalResult.ResultMessage
    : $"[ERROR] {finalResult.ErrorMessage}\n{string.Join("\n", finalResult.ErrorDetails ?? Array.Empty<string>())}";

                    byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                    await pipeServer.WriteAsync(BitConverter.GetBytes(responseBytes.Length), 0, 4, cancellationToken);
                    await pipeServer.WriteAsync(responseBytes, 0, responseBytes.Length, cancellationToken);
                    await pipeServer.FlushAsync(cancellationToken);

                    File.AppendAllText(_logPath, $"Sent response to bridge: {response}\n");
                }
                catch (OperationCanceledException)
                {
                    File.AppendAllText(_logPath, $"Connection cancelled - {DateTime.Now}\n");
                    break;
                }
                catch (Exception ex)
                {
                    File.AppendAllText(_logPath, $"Unhandled server error: {ex.Message} - {DateTime.Now}\n");
                }
                finally
                {
                    if (pipeServer.IsConnected)
                    {
                        pipeServer.Disconnect();
                        File.AppendAllText(_logPath, $"Pipe disconnected - {DateTime.Now}\n");
                    }
                    else
                    {
                        File.AppendAllText(_logPath, "Pipe never connected\n");
                    }
                }
            }

            File.AppendAllText(_logPath, $"Server loop stopped: {DateTime.Now}\n");
        }

        public void Stop()
        {
            if (!_running) return;
            _running = false;
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            File.AppendAllText(_logPath, $"Server stopped: {DateTime.Now}\n");
        }

        public bool IsRunning() => _running;
    }
}