using Server.ViewModels;
using System.Windows;

namespace Server
{
    internal class AppBootstrapper : Caliburn.Micro.BootstrapperBase
    {
        public AppBootstrapper()
        {
            Initialize();
        }

        protected override void OnStartup(object sender, StartupEventArgs e)
        {
            DisplayRootViewFor<ServerViewModel>();
        }
    }
}