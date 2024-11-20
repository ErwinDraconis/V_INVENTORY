using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace ValeoItacCheck
{
    /// <summary>
    /// Interaction logic for SinglePcbView.xaml
    /// </summary>
    public partial class SinglePcbView : UserControl, INotifyPropertyChanged
    {
        #region Private variables

        private readonly IIMSApi _imsapi;

        private readonly IMessageService _messageService;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private DispatcherTimer _connectionTimer;
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

        private bool _isToggleEnabled = true;

        public bool isToggleEnabled
        {
            get { return _isToggleEnabled; }
            set { _isToggleEnabled = value; OnPropertyChanged(nameof(isToggleEnabled)); }
        }

        private Visibility _isResultGridVisible = Visibility.Collapsed;

        public Visibility isResultGridVisible
        {
            get { return _isResultGridVisible; }
            set { _isResultGridVisible = value; OnPropertyChanged(nameof(isResultGridVisible)); }
        }

        private SinglePCBRslt _pcbRslt = new SinglePCBRslt();

        public SinglePCBRslt PcbRslt
        {
            get { return _pcbRslt; }
            set { _pcbRslt = value; OnPropertyChanged(nameof(PcbRslt)); }
        }

        private string _containerName = string.Empty;

        public string ContainerName
        {
            get { return _containerName; }
            set { _containerName = value; OnPropertyChanged(nameof(ContainerName)); }
        }

        #endregion

        #region Constructor

        public SinglePcbView(IIMSApi imsapi, IMessageService messageService)
        {
            InitializeComponent();

            this.DataContext = this;

            _imsapi = imsapi;
            _messageService = messageService;
            Loaded += SinglePcbView_Loaded;
            Unloaded += SinglePcbView_Unloaded;
            OnBtnContainer = new RelayCommand(OnContainerEnabled);
            OffBtnContainer = new RelayCommand(OnContainerDisabled);
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

        private void SinglePcbView_Loaded(object sender, RoutedEventArgs e)
        {
            ContainerName = AppSettings.CONTAINER_NAME;

            txtContainer.Focus();

            InitializeConnectionTimer();
        }

        private void SinglePcbView_Unloaded(object sender, RoutedEventArgs e)
        {
            _connectionTimer.Stop();
            Application.Current.Shutdown();
        }

        private async void txtSNR_KeyDown(object sender, KeyEventArgs e)
        {
            isTxtEnabled = false;
            if (e.Key == Key.Enter)
            {
                if(SNR.Length > 3)
                {
                    await GetPCBData();
                    SNR = string.Empty;
                    txtSNR.Focus();
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

        private async Task GetPCBData()
        {
            PcbRslt.clear(); isResultGridVisible = Visibility.Collapsed;
#if RELEASE
            // 1: Check if booking is for Board only (board in panel not allowed) 
            var panelResult = await _imsapi.GetPanelSNStateAsync(AppSettings.iTAC_Station, SNR);

            if (panelResult != null && panelResult.Count > 1)
            {
                string error = "Panel Booking is not allowed in this menu, Please scan SN of a PCB only.";
                await _messageService.ShowMessage("Error", error);
                return;
            }
#endif
            // 2: Check SN State
            try
            {
                (bool result, string snState, int errorCode) = await _imsapi.CheckSerialNumberStateAsync(AppSettings.iTAC_Station, SNR);
                if (int.TryParse(snState, out int status))
                    PcbRslt.Status = status;
                else
                    PcbRslt.Status = -1;
            }
            catch (Exception ex)
            {
                Log.Error($"Exception occured in class SinglePcbView function {nameof(GetPCBData)}, error {ex.Message}");
                PcbRslt.Status = -1;
            }
            
            // Get SAP_PN and PCB Coefficient for each PCB
            if(!await GetSAPDataAsync())
            {
                return;
            }

            // Write data into Csv file
            CsvHelper.WriteToCsv(new List<PanelPositions>
                {
                    new PanelPositions
                    {
                        Container = ContainerText,
                        SerialNumber = SNR,
                        PartNumber = PcbRslt.PartNumber,
                        SapPn = PcbRslt.SapPn,
                        PcbCoef = PcbRslt.PcbCoef
                    }
                });
            isResultGridVisible = Visibility.Visible;
        }

        private async Task<bool> GetSAPDataAsync()
        {
            string productPN = string.Empty;

            try
            {
                if (SNR.Substring(0, 4) == "TG01")
                {
                    productPN = SNR.Substring(5, 10).TrimStart('0');
                }
                else if (SNR.Substring(0, 4) == "L000")
                {
                    productPN = SNR.Substring(4, 10).TrimStart('0');
                }
                else
                {
                    await _messageService.ShowMessage("SN Format Error : " + SNR, "SNR format is invalid or too short.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Exception occurred in class SinglePcbView function {nameof(GetSAPDataAsync)}: {ex.Message}");
                await _messageService.ShowMessage("SN Format Error", "SN Format Error, please enter a valid SN.");
                return false;
            }

            var data = GetLabelData.GetDataForLabel(productPN);

            if (data == null)
            {
                string error = $"Product Data for PN [{productPN}] not found in bin.txt file.";
                Log.Warn($"Data not found for PN: {productPN}");
                await _messageService.ShowMessage("Error", error);
                return false;
            }

            // Populate the PcbRslt object with the retrieved data
            PcbRslt.SapPn      = data.SAP_PN.TrimStart('0');
            PcbRslt.PcbCoef    = data.PCB_COEFFICIENT;
            PcbRslt.PartNumber = productPN.TrimStart('0');
            PcbRslt.Container  = ContainerText;
            PcbRslt.SerialNumber = SNR;

            return true;
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
                    isResultGridVisible = Visibility.Collapsed;
                    isTxtContainerEnabled = false;
                    isTxtEnabled = false;
                    Toggle.IsEnabled = false;
                    await _messageService.ShowMessage("iTAC connection Error", "No Connection could be established with iTAC server");
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
