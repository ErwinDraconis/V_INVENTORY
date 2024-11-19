using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ValeoItacCheck
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    public class StatusToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int status)
            {
                switch (status)
                {
                    case 0:
                        return "Ok";
                    case 1:
                        return "Fail";
                    case 2:
                        return "Scrap";
                    case 10:
                        return "iTAC or MES Failed";
                    default:
                        return "Unknown";
                }
            }
            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
