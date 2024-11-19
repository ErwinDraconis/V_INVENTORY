using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValeoItacCheck
{
    public interface IMessageService
    {
        Task ShowMessage(string title, string message);
    }
}
