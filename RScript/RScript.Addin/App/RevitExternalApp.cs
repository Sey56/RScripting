using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RScript.Addin.Services;
using System;

namespace RScript.Addin.App
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class RevitExternalApp : IExternalApplication
    {
        public static string HomePath => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        private static RScriptServer _server;
        private static bool _serverRunning;
        private static  PushButton _toggleButton;

        public Result OnStartup(UIControlledApplication application)
        {
            AddRibbonButton(application);
            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            _server?.Stop();
            _serverRunning = false;
            UpdateButtonState();
            return Result.Succeeded;
        }

        private void AddRibbonButton(UIControlledApplication application)
        {
            RibbonPanel panel = application.CreateRibbonPanel("RScript");
            PushButtonData buttonData = new PushButtonData(
                "ToggleRScriptServer",
                "RScript Server\n(Off)",
                typeof(RevitExternalApp).Assembly.Location,
                typeof(RScript.Addin.Commands.ToggleServerCommand).FullName)
            {
                ToolTip = "Toggle the RScript server to run scripts from VSCode."
            };
            _toggleButton = panel.AddItem(buttonData) as PushButton;
        }

        public static bool ServerRunning => _serverRunning;
        public static void SetServerRunning(bool running)
        {
            _serverRunning = running;
            UpdateButtonState();
        }

        public static RScriptServer Server => _server;
        public static void SetServer(RScriptServer server) => _server = server;

        private static void UpdateButtonState()
        {
            if (_toggleButton != null)
            {
                _toggleButton.ItemText = _serverRunning ? "RScript Server\n(On)" : "RScript Server\n(Off)";
                _toggleButton.ToolTip = _serverRunning
                    ? "Server is running. Click to stop."
                    : "Server is stopped. Click to start.";
            }
        }
    }
}