using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ValeoItacCheck
{
    public class ToggleContentTemplateSelector: DataTemplateSelector
    {
        public DataTemplate OffTemplate { get; set; }
        public DataTemplate OnTemplate { get; set; }
        public override DataTemplate SelectTemplate(object item, DependencyObject container) 
        { 
            var toggleSwitch = container as ToggleSwitch; 
            if (toggleSwitch != null && toggleSwitch.IsOn) 
            { 
                return OnTemplate; 
            } 
            return OffTemplate;
        }
    }
}
