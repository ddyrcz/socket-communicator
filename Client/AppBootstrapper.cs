using Client.ViewModels;
using System.Windows;

namespace Client
{
    internal class AppBootstrapper : Caliburn.Micro.BootstrapperBase
    {
        public AppBootstrapper()
        {
            Initialize();
        }

        protected override void OnStartup(object sender, StartupEventArgs e)
        {
            DisplayRootViewFor<ClientViewModel>();
        }
    }
}