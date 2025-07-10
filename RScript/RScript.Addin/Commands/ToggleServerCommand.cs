using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RScript.Addin.App;
using RScript.Addin.Services;
using RScript.Addin.ViewModels;

namespace RScript.Addin.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class ToggleServerCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            if (commandData == null)
            {
                message = "Command data is null.";
                return Result.Failed;
            }

            if (!RevitExternalApp.ServerRunning)
            {
                var server = new RScriptServer(commandData.Application);
                var actionHandler = new RevitActionHandler(MainViewModel.Instance);
                var codeExecutionEvent = ExternalEvent.Create(actionHandler);
                MainViewModel.Instance.Initialize(codeExecutionEvent);
                server.Start();
                RevitExternalApp.SetServer(server);
                RevitExternalApp.SetServerRunning(true);
                TaskDialog.Show("RScript", "Server started. Run scripts from VSCode!");
            }
            else
            {
                // Stop the current server
                RevitExternalApp.Server?.Stop();
                RevitExternalApp.SetServer(null); // Clear the old server reference
                RevitExternalApp.SetServerRunning(false);
                TaskDialog.Show("RScript", "Server stopped.");

                // Do not create a new server instance until the user starts it again
            }

            return Result.Succeeded;
        }
    }
}