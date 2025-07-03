using Autodesk.Revit.UI;
using RScript.Addin.Services;
using RScript.Addin.ViewModels;
using RScript.Addin.App;
using Autodesk.Revit.DB;

namespace RScript.Addin.Commands
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class ToggleServerCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiApp = commandData.Application;

            var actionHandler = new RevitActionHandler(MainViewModel.Instance);
            var codeExecutionEvent = ExternalEvent.Create(actionHandler);
            MainViewModel.Instance.Initialize(codeExecutionEvent);

            try
            {
                if (RevitExternalApp.ServerRunning)
                {
                    RevitExternalApp.Server?.Stop();
                    RevitExternalApp.SetServerRunning(false);
                    try
                    {
                        TaskDialog.Show("RScript", "Server stopped.");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"TaskDialog error: {ex.Message}");
                    }
                }
                else
                {
                    var scriptServer = new RevitScriptServer(uiApp);
                    RevitExternalApp.SetServer(scriptServer);
                    scriptServer.Start();
                    RevitExternalApp.SetServerRunning(true);
                    try
                    {
                        TaskDialog.Show("RScript", "Server started.");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"TaskDialog error: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("RScript Error", $"Failed to toggle server: {ex.Message}");
                return Result.Failed;
            }

            return Result.Succeeded;
        }
    }
}