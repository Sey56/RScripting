using Autodesk.Revit.UI;
using RScript.Addin.Services;
using System;
using System.IO;

namespace RScript.Addin.ViewModels
{
    public class MainViewModel
    {
        private ExternalEvent _codeExecutionEvent;
        private string _pendingScriptContent;
        private UIApplication _pendingUiApp;

        public static MainViewModel Instance => _instance ??= new MainViewModel();
        private static MainViewModel _instance;

        public event Action<ExecutionResult> OnExecutionComplete;

        private MainViewModel() { }

        public void Initialize(ExternalEvent codeExecutionEvent)
        {
            _codeExecutionEvent = codeExecutionEvent;
        }

        public ExecutionResult QueueScriptFromServer(string scriptContent, UIApplication uiApp)
        {
            _pendingScriptContent = scriptContent;
            _pendingUiApp = uiApp;

            if (_codeExecutionEvent == null)
            {
                var errorMessage = "External event is not initialized.";
                LogErrorToFile(errorMessage);
                return new ExecutionResult { IsSuccess = false, ErrorMessage = errorMessage };
            }

            _codeExecutionEvent.Raise();
            return new ExecutionResult { IsSuccess = true, ResultMessage = "Script queued for execution." };
        }

        public ExecutionResult ExecuteCodeInRevit(UIApplication uiApp)
        {
            ExecutionResult result;

            try
            {
                if (string.IsNullOrEmpty(_pendingScriptContent) || _pendingUiApp == null)
                {
                    var errorMessage = "No script content or UIApplication available to execute.";
                    LogErrorToFile(errorMessage);
                    result = new ExecutionResult { IsSuccess = false, ErrorMessage = errorMessage };
                }
                else
                {
                    result = CodeRunner.ExecuteCode(_pendingScriptContent, _pendingUiApp);
                    if (!result.IsSuccess)
                        LogErrorToFile(result.ErrorMessage ?? "Unknown error.");
                }
            }
            catch (Exception ex)
            {
                var error = $"Runtime error: {ex.Message}";
                LogErrorToFile(error);
                result = new ExecutionResult { IsSuccess = false, ErrorMessage = error };
            }
            finally
            {
                _pendingScriptContent = null;
                _pendingUiApp = null;
            }

            // 🔔 Notify listeners (like RScriptServer)
            OnExecutionComplete?.Invoke(result);

            return result;
        }

        private static void LogErrorToFile(string errorMessage)
        {
            var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "CodeEditorError.txt");
            try
            {
                File.WriteAllText(logPath, $"{DateTime.Now}: {errorMessage}\n");
            }
            catch { /* Fail silently */ }
        }
    }
}