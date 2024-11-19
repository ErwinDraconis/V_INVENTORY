using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Unity;

namespace ValeoItacCheck
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow ,INotifyPropertyChanged, IMessageService
    {
        #region private variables

        

        private readonly IIMSApi _imsapi;

        private readonly IniReader _iniReader;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        #endregion

        #region Properties

        private MainWindowModel _model;

        public MainWindowModel Model
        {
            get { return _model; }
            set 
            { 
                _model = value;
                OnPropertyChanged(nameof(Model));
            }
        }

        #endregion

        #region Class constructor

        public MainWindow(IIMSApi imsapi, IniReader iniReader)
        {
            InitializeComponent();

            ConfigureLogger();

            _imsapi    = imsapi;
            _iniReader = iniReader;
            Model      = new MainWindowModel();

            this.DataContext = this;
            Loaded += MainWindow_Loaded;
            
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ConnectToItac();
        }

        #endregion

        #region Private methodes

        private async void ConnectToItac()
        {
            // Retrieve settings from the INI file
            try
            {
                AppSettings.iTAC_Server     = _iniReader.GetSetting("Settings", "iTAC_Server");
                AppSettings.iTAC_Station    = _iniReader.GetSetting("Settings", "iTAC_Station");
                AppSettings.iTAC_USER_NAME  = _iniReader.GetSetting("Settings", "iTAC_USER_NAME");
                AppSettings.iTAC_USER_PW    = _iniReader.GetSetting("Settings", "iTAC_USER_PW");
                AppSettings.STATION_TYPE    = _iniReader.GetSetting("Settings", "STATION_TYPE");
                AppSettings.CONTAINER_NAME  = _iniReader.GetSetting("Settings", "CONTAINER_NAME");
                AppSettings.SERVER_CNX_TIMEOUT = Convert.ToInt32(_iniReader.GetSetting("Settings", "SERVER_CNX_TIMEOUT"));
            }
            catch (Exception ex)
            {
                await this.ShowMessageAsync("Error reading ini file", ex.Message);
                return;
            }

            if (string.IsNullOrWhiteSpace(AppSettings.iTAC_Server)    ||
                string.IsNullOrWhiteSpace(AppSettings.iTAC_Station)   ||
                string.IsNullOrWhiteSpace(AppSettings.iTAC_USER_NAME) ||
                string.IsNullOrWhiteSpace(AppSettings.iTAC_USER_PW)   ||
                string.IsNullOrWhiteSpace(AppSettings.STATION_TYPE)   ||
                string.IsNullOrWhiteSpace(AppSettings.CONTAINER_NAME) )
            {
                await this.ShowMessageAsync("Configuration Error", "One or more required settings are missing. Please check your configuration.");
                return;
            }

            Model.ItacStation = AppSettings.iTAC_Station;
            Model.ItacServer = AppSettings.iTAC_Server;
            Model.ItacUser = AppSettings.iTAC_USER_NAME;
            Model.AppbuildVersion = V_INVENTORY.Properties.Settings.Default.BuildNumber;
            Model.CompanyName = V_INVENTORY.Properties.Settings.Default.Company;

            if (!GetLabelData.isBinFileAvailable())
            {
                await this.ShowMessageAsync("Error, file missing", "Bin file is not located at its location under ..\\data\\bin.txt");
                return;
            }

            // Check connection with timeout
            bool connectionResult = await Task.Run(() => ConnectWithTimeout());

            if (!connectionResult)
            {
                await this.ShowMessageAsync("Connection Error", "Could not connect to iTAC server within the timeout period. Server at " + AppSettings.iTAC_Server);
            }
            else
            {
                LoadView();
            }
        }

        /// <summary>
        /// Tries to connect to the iTAC server, enforcing a 5-second timeout.
        /// </summary>
        /// <returns>True if connected, false if timed out or failed.</returns>
        private bool ConnectWithTimeout()
        {
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(AppSettings.SERVER_CNX_TIMEOUT));

                try
                {
                    var task = Task.Run(() =>
                    {
                        // Attempt connection
                        return _imsapi.ItacConnection();
                    }, cancellationTokenSource.Token);

                    return task.Wait(TimeSpan.FromSeconds(AppSettings.SERVER_CNX_TIMEOUT)) && task.Result;
                }
                catch (OperationCanceledException)
                {
                    return false;
                }
                catch (AggregateException ex) when (ex.InnerExceptions.Any(e => e is TaskCanceledException))
                {
                    return false;
                }
            }
        }


        private void LoadView()
        {
            switch (AppSettings.STATION_TYPE.ToLower())
            {
                case "panel":
                    var panelView = App.Container.Resolve<PanelView>();
                    MainShellRegion.Content = panelView;
                    break;
                case "pcb":
                    var pcbView = App.Container.Resolve<SinglePcbView>();
                    MainShellRegion.Content = pcbView;
                    break;
                default:
                    break;
            }
        }

        private void ConfigureLogger()
        {
            var logDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
        }

        public async Task ShowMessage(string title, string message)
        {
            await this.ShowMessageAsync(title, message); 
        }

        #endregion

        #region Notification boiler plate

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}