using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KioskClient
{
    public class MainPageArguments
    {
        public bool ShowSetupInformation { get; set; }

        public MainPageArguments(bool showSetupInformation = false)
        {
            ShowSetupInformation = showSetupInformation;
        }
    }
}
