using Autodesk.Revit.UI;
using RScript.Addin.Services;
using System.IO;

namespace RScript.Addin.ViewModels
{
    public class MainViewModel
    {
        private ExternalEvent _codeExecutionEvent;
        private string _pendingScriptContent;
        private UIApplication _pendingUiApp;

        public static MainViewModel Instance => _instance ??= new MainViewModel();
        private static MainViewModel? _instance;

        private MainViewModel()
        {
        }

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
                ScriptGlobals.Print($"❌ {errorMessage}");
                return new ExecutionResult { IsSuccess = false, ErrorMessage = errorMessage };
            }

            _codeExecutionEvent.Raise();
            return new ExecutionResult { IsSuccess = true, ResultMessage = "Script queued for execution." };
        }

        public ExecutionResult ExecuteCodeInRevit(UIApplication uiApp)
        {
            try
            {
                if (string.IsNullOrEmpty(_pendingScriptContent) || _pendingUiApp == null)
                {
                    var errorMessage = "No script content or UIApplication available to execute.";
                    LogErrorToFile(errorMessage);
                    ScriptGlobals.Print($"❌ {errorMessage}");
                    return new ExecutionResult { IsSuccess = false, ErrorMessage = errorMessage };
                }

                var result = CodeRunner.ExecuteCode(_pendingScriptContent, _pendingUiApp);

                if (!result.IsSuccess)
                {
                    LogErrorToFile(result.ErrorMessage ?? "Unknown error.");
                    ScriptGlobals.Print($"❌ {result.ErrorMessage}");
                }
                else
                {
                    ScriptGlobals.Print($"✅ {result.ResultMessage}");
                }

                return result;
            }
            catch (Exception ex)
            {
                LogErrorToFile(ex.Message);
                ScriptGlobals.Print($"🔥 Unexpected runtime error: {ex.Message}");
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Runtime error: {ex.Message}" };
            }
            finally
            {
                _pendingScriptContent = null;
                _pendingUiApp = null;
                ScriptGlobals.OutputPipeName = null;
            }
        }

        private static void LogErrorToFile(string errorMessage)
        {
            var errorLogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "CodeEditorError.txt");
            try
            {
                File.WriteAllText(errorLogPath, $"{DateTime.Now}: {errorMessage}\n");
            }
            catch (Exception ex)
            {
                ScriptGlobals.Print($"⚠️ Failed to write error log: {ex.Message}");
            }
        }
    }
}