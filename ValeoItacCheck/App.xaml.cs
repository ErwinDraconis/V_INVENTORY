using System;
using System.Windows;
using Unity;
using Unity.Injection;

namespace ValeoItacCheck
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IUnityContainer Container { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Initialize and configure the container
            Container = new UnityContainer();
            ConfigureServices(Container);

            // Resolve and show the main window
            var mainWindow = Container.Resolve<MainWindow>();
            mainWindow.Show();
        }

        private void ConfigureServices(IUnityContainer container)
        {
            // Register services and views
            container.RegisterSingleton<IIMSApi, IMSApi>();
            container.RegisterSingleton<IniReader>(new InjectionConstructor("AppSetting.ini"));
            container.RegisterSingleton<MainWindow>();
            container.RegisterType<PanelView>();
            container.RegisterType<SinglePcbView>();
            container.RegisterSingleton<IMessageService, MainWindow>();
        }
    }

}
