namespace KioskLibrary.PageArguments
{
    public class MainPageArguments
    {
        public bool ShowSetupInformation { get; set; }

        public MainPageArguments(bool showSetupInformation = false) => ShowSetupInformation = showSetupInformation;
    }
}
