using System.ComponentModel;

namespace ValeoItacCheck
{
    public class SinglePCBRslt : INotifyPropertyChanged
    {
        private string _serialNumber;
        private string _partNumber;
        private int _status;
        private string _sapPn;
        private string _pcbCoef;
        private string _container;


        public string SerialNumber
        {
            get => _serialNumber;
            set
            {
                if (_serialNumber != value)
                {
                    _serialNumber = value;
                    OnPropertyChanged(nameof(SerialNumber));
                }
            }
        }

        public int Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        public string SapPn
        {
            get => _sapPn;
            set
            {
                if (_sapPn != value)
                {
                    _sapPn = value;
                    OnPropertyChanged(nameof(SapPn));
                }
            }
        }

        public string PcbCoef
        {
            get => _pcbCoef;
            set
            {
                if (_pcbCoef != value)
                {
                    _pcbCoef = value;
                    OnPropertyChanged(nameof(PcbCoef));
                }
            }
        }

        public string Container
        {
            get => _container;
            set
            {
                if (_container != value)
                {
                    _container = value;
                    OnPropertyChanged(nameof(Container));
                }
            }
        }

        public string PartNumber
        {
            get => _partNumber;
            set
            {
                if (_partNumber != value)
                {
                    _partNumber = value;
                    OnPropertyChanged(nameof(PartNumber));
                }
            }
        }

        public void clear()
        {
            SerialNumber = string.Empty; 
            Status = 0; 
            SapPn = string.Empty; 
            PcbCoef = string.Empty; 
            Container = string.Empty;
            PartNumber = string.Empty;      
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
