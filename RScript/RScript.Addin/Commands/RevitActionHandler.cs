using Autodesk.Revit.UI;
using RScript.Addin.ViewModels;

namespace RScript.Addin.Commands
{
    public class RevitActionHandler : IExternalEventHandler
    {
        private readonly MainViewModel _viewModel;

        public RevitActionHandler(MainViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public void Execute(UIApplication app)
        {
            _viewModel.ExecuteCodeInRevit(app);
        }

        public string GetName() => "RScript Code Executor";
    }
}