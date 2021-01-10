using System.Collections.Generic;

namespace KioskLibrary.PageArguments
{
    public class SettingsPageArguments
    {
        public List<string> Log { get; set; }

        public SettingsPageArguments(List<string> log) => Log = log;
    }
}
