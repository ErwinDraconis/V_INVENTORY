using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValeoItacCheck
{
    public class MainWindowModel : INotifyPropertyChanged
    {
        private string _itacServer;

        public string ItacServer
        {
            get { return _itacServer; }
            set { _itacServer = value; OnPropertyChanged(nameof(ItacServer)); }
        }

        private string _itacStation;

        public string ItacStation
        {
            get { return _itacStation; }
            set { _itacStation = value; OnPropertyChanged(nameof(ItacStation)); }
        }

        private string _itacUser;

        public string ItacUser
        {
            get { return _itacUser; }
            set { _itacUser = value; OnPropertyChanged(nameof(ItacUser)); }
        }

        private string _AppbuildVersion;

        public string AppbuildVersion
        {
            get { return _AppbuildVersion; }
            set { _AppbuildVersion = value; OnPropertyChanged(nameof(AppbuildVersion)); }
        }

        private string _companyName;

        public string CompanyName
        {
            get { return _companyName; }
            set { _companyName = value; OnPropertyChanged(nameof(CompanyName)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
