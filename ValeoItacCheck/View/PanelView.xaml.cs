using MahApps.Metro.Controls;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace ValeoItacCheck
{
    /// <summary>
    /// Interaction logic for PanelView.xaml
    /// </summary>
    public partial class PanelView : UserControl, INotifyPropertyChanged
    {
        #region Private variables

        private readonly IIMSApi _imsapi;

        private readonly IMessageService _messageService;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private DispatcherTimer _connectionTimer;
        #endregion

        #region class constructor

        public PanelView(IIMSApi imsapi, IMessageService messageService)
        {
            InitializeComponent();

            this.DataContext = this;

            _imsapi = imsapi;
            _messageService = messageService;
            Loaded += PanelView_Loaded;
            Unloaded += PanelView_Unloaded;
            Positions = new ObservableCollection<PanelPositions>();

            OnBtnContainer = new RelayCommand(OnContainerEnabled);
            OffBtnContainer = new RelayCommand(OnContainerDisabled);
        }

        #endregion

        #region Properties

        private string _snr;
        public string SNR
        {
            get { return _snr; }
            set { _snr = value; OnPropertyChanged(nameof(SNR)); }
        }

        private bool _isTxtEnabled = false;

        public bool isTxtEnabled
        {
            get { return _isTxtEnabled; }
            set { _isTxtEnabled = value; OnPropertyChanged(nameof(isTxtEnabled)); }
        }

        private ObservableCollection<PanelPositions> _positions;
        public ObservableCollection<PanelPositions> Positions
        {
            get => _positions;
            set { _positions = value; OnPropertyChanged(nameof(Positions)); }
        }

        private string _containerText;
        public string ContainerText
        {
            get { return _containerText; }
            set { _containerText = value; OnPropertyChanged(nameof(ContainerText)); }
        }

        private bool _isTxtContainerEnabled = true;

        public bool isTxtContainerEnabled
        {
            get { return _isTxtContainerEnabled; }
            set { _isTxtContainerEnabled = value; OnPropertyChanged(nameof(isTxtContainerEnabled)); }
        }

        //
        private bool _isToggleEnabled = true;

        public bool isToggleEnabled
        {
            get { return _isToggleEnabled; }
            set { _isToggleEnabled = value; OnPropertyChanged(nameof(isToggleEnabled)); }
        }

        // ContainerName
        private string _containerName = string.Empty;

        public string ContainerName
        {
            get { return _containerName; }
            set { _containerName = value; OnPropertyChanged(nameof(ContainerName)); }
        }
        #endregion

        #region Commands & handlers

        public ICommand OnBtnContainer { get; }
        public ICommand OffBtnContainer { get; }


        private void OnContainerEnabled(object parameter)
        {
            isTxtContainerEnabled = true;
            txtContainer.Focus();
            SNR = string.Empty;
            isTxtEnabled = false;
            txtContainer.Text = string.Empty;
            ContainerText = string.Empty;
        }

        private void OnContainerDisabled(object parameter)
        {
            isTxtContainerEnabled = false;
            if (!string.IsNullOrEmpty(ContainerText))
            {
                isTxtEnabled = true;
                txtSNR.Focus();
            }
            else
            {
                isTxtEnabled = false;
            }
            SNR = string.Empty;
        }

        #endregion

        #region events

        private void PanelView_Loaded(object sender, RoutedEventArgs e)
        {
            ContainerName = AppSettings.CONTAINER_NAME;

            txtContainer.Focus();

            InitializeConnectionTimer();
        }

        private void PanelView_Unloaded(object sender, RoutedEventArgs e)
        {
            _connectionTimer.Stop();
        }

        private async void txtSNR_KeyDown(object sender, KeyEventArgs e)
        {
            isTxtEnabled = false;
            if (e.Key == Key.Enter)
            {
                if(SNR.Length > 3)
                {
                    await LoadPositions();
                }
            }
            isTxtEnabled = true;
        }

        private void txtContainer_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OnContainerDisabled(sender);
                isToggleEnabled = false;
            }
        }

        #endregion

        #region Methodes

        private async Task LoadPositions()
        {
            try
            {
                Positions.Clear();
                var panelsResult = await _imsapi.GetPanelSNStateAsync(AppSettings.iTAC_Station, SNR);

                if (panelsResult == null || panelsResult.Count == 0)
                {
                    string error = $"There is no panel record found for this SN [{SNR}]. An empty list is returned: count {panelsResult?.Count}";
                    await _messageService.ShowMessage("Error", error);

                    return;
                }

                if (panelsResult.Count == 1)
                {
                    string error = "PCB does not belong to Panel anymore, this configuration is not allowed!";
                    await _messageService.ShowMessage("Error", error);
                    return;
                }

                // Populate the view first (so to let the user see which PCB is Ok and which is not)
                foreach (var pan in panelsResult)
                {
                    Positions.Add(pan);
                }

                // Get SAP_PN and PCB Coefficient for each PCB
                await GetSAPDataAsync(panelsResult);

                // Write data into Csv file
                CsvHelper.WriteToCsv(panelsResult);
            }
            catch (Exception ex) 
            {
                Log.Error($"Exception occured in class PanelView function {nameof(LoadPositions)},error : {ex.Message}");
                await _messageService.ShowMessage("Exception Occured !", ex.Message);
            }
        }

        private async Task GetSAPDataAsync(List<PanelPositions> panel)
        {
            foreach (var pcb in panel)
            {
                string productPN = string.Empty;

                if (pcb.SerialNumber.Substring(0, 4) == "TG01")
                {
                    productPN = pcb.SerialNumber.Substring(5, 10).TrimStart('0');
                }
                else if (pcb.SerialNumber.Substring(0, 4) == "L000")
                {
                    productPN = pcb.SerialNumber.Substring(4, 10).TrimStart('0');
                }
                else
                {
                    await _messageService.ShowMessage("SN Format Error : " + pcb.SerialNumber, "SNR format is invalid or too short.");
                }

                var data = GetLabelData.GetDataForLabel(productPN);

                if (data == null)
                {
                    string error = $"Product Data for PN [{productPN}] not found in bin.txt file";
                    await _messageService.ShowMessage("Error", error);
                }
                else
                {
                    var panelToUpdate = panel.FirstOrDefault(x => x.SerialNumber == pcb.SerialNumber);
                    if (panelToUpdate != null)
                    {
                        panelToUpdate.SapPn      = data.SAP_PN.TrimStart('0');
                        panelToUpdate.PcbCoef    = data.PCB_COEFFICIENT;
                        panelToUpdate.PartNumber = productPN.TrimStart('0');
                        panelToUpdate.Container  = ContainerText;
                    }
                }
            }
        }

        private void InitializeConnectionTimer()
        {
            _connectionTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };

            _connectionTimer.Tick += CheckItacConnectionStatus;

            _connectionTimer.Start();
        }

        private async void CheckItacConnectionStatus(object sender, EventArgs e)
        {
            try
            {
                if (!await _imsapi.CheckItacConnectionAsync())
                {
                    isTxtContainerEnabled = false;
                    isTxtEnabled = false;
                    Toggle.IsEnabled = false;
                    Positions.Clear();
                    await _messageService.ShowMessage("iTAC connection Error", "No Connection could be established with iTAC server : " + AppSettings.iTAC_Server);
                    
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error checking connection: {ex.Message}");
            }
        }
        #endregion

        #region Notification

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        
    }
}
